/*
MIT License - Based on the PHD2 client implementation by Andy Galasso
https://github.com/agalasso/phd2client

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TouchNStars.PHD2
{
    // Settling progress information returned by CheckSettling()
    public class SettleProgress
    {
        public bool Done { get; set; }
        public double Distance { get; set; }
        public double SettlePx { get; set; }
        public double Time { get; set; }
        public double SettleTime { get; set; }
        public int Status { get; set; }
        public string Error { get; set; }
    }

    public class GuideStats
    {
        public double RmsTotal { get; set; }
        public double RmsRA { get; set; }
        public double RmsDec { get; set; }
        public double PeakRA { get; set; }
        public double PeakDec { get; set; }

        public GuideStats Clone() { return (GuideStats)MemberwiseClone(); }
    }

    public class PHD2Exception : ApplicationException
    {
        public PHD2Exception(string message) : base(message) { }
        public PHD2Exception(string message, Exception inner) : base(message, inner) { }
    }

    public class StarLostInfo
    {
        public int Frame { get; set; }
        public double Time { get; set; }
        public double StarMass { get; set; }
        public double SNR { get; set; }
        public double AvgDist { get; set; }
        public int ErrorCode { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PHD2Status
    {
        public string AppState { get; set; }
        public double AvgDist { get; set; }
        public GuideStats Stats { get; set; }
        public string Version { get; set; }
        public string PHDSubver { get; set; }
        public bool IsConnected { get; set; }
        public bool IsGuiding { get; set; }
        public bool IsSettling { get; set; }
        public SettleProgress SettleProgress { get; set; }
        public StarLostInfo LastStarLost { get; set; }
    }

    internal class PHD2Connection : IDisposable
    {
        private TcpClient tcpClient;
        private StreamWriter streamWriter;
        private StreamReader streamReader;

        public bool Connect(string hostname, ushort port)
        {
            try
            {
                tcpClient = new TcpClient(hostname, port);
                streamWriter = new StreamWriter(tcpClient.GetStream())
                {
                    AutoFlush = true,
                    NewLine = "\r\n"
                };
                streamReader = new StreamReader(tcpClient.GetStream());
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        public bool IsConnected => tcpClient?.Connected ?? false;

        public string ReadLine()
        {
            try
            {
                return streamReader?.ReadLine();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void WriteLine(string line)
        {
            streamWriter?.WriteLine(line);
        }

        public void Terminate()
        {
            tcpClient?.Close();
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            streamWriter?.Dispose();
            streamReader?.Dispose();
            tcpClient?.Close();
            tcpClient?.Dispose();
        }
    }

    internal class Accumulator
    {
        private readonly List<double> values = new List<double>();

        public void Add(double value)
        {
            values.Add(value);
        }

        public void Reset()
        {
            values.Clear();
        }

        public double Stdev()
        {
            if (values.Count < 2) return 0.0;

            double sum = 0.0;
            double sumSquares = 0.0;

            foreach (var value in values)
            {
                sum += value;
                sumSquares += value * value;
            }

            double mean = sum / values.Count;
            double variance = (sumSquares - sum * mean) / (values.Count - 1);
            return Math.Sqrt(Math.Max(0.0, variance));
        }

        public double Peak()
        {
            double peak = 0.0;
            foreach (var value in values)
            {
                if (Math.Abs(value) > peak)
                    peak = Math.Abs(value);
            }
            return peak;
        }
    }

    public class PHD2Client : IDisposable
    {
        private readonly string hostname;
        private readonly uint instance;
        private PHD2Connection connection;
        private Thread workerThread;
        private volatile bool terminate;
        private readonly object syncObject = new object();
        private JObject response;

        private readonly Accumulator accumRA = new Accumulator();
        private readonly Accumulator accumDec = new Accumulator();
        private bool accumActive;
        private double settlePx;

        // Current state
        public string AppState { get; private set; } = "Stopped";
        public double AvgDist { get; private set; }
        public GuideStats Stats { get; private set; } = new GuideStats();
        public string Version { get; private set; }
        public string PHDSubver { get; private set; }
        public StarLostInfo LastStarLost { get; private set; }
        private SettleProgress settle;

        public PHD2Client(string hostname = "localhost", uint instance = 1)
        {
            this.hostname = hostname;
            this.instance = instance;
            this.connection = new PHD2Connection();
        }

        public bool IsConnected => connection?.IsConnected ?? false;

        public void Connect()
        {
            Disconnect();

            ushort port = (ushort)(4400 + instance - 1);
            if (!connection.Connect(hostname, port))
                throw new PHD2Exception($"Could not connect to PHD2 instance {instance} on {hostname}");

            terminate = false;
            workerThread = new Thread(Worker) { IsBackground = true };
            workerThread.Start();
        }

        public void Disconnect()
        {
            if (workerThread != null)
            {
                terminate = true;
                connection?.Terminate();
                workerThread.Join(5000);
                workerThread = null;
            }

            connection?.Close();
            connection = new PHD2Connection();
        }

        public JObject Call(string method, JToken param = null)
        {
            string jsonRpc = MakeJsonRpc(method, param);
            Debug.WriteLine($"PHD2 Call: {jsonRpc}");

            connection.WriteLine(jsonRpc);

            lock (syncObject)
            {
                while (response == null)
                    Monitor.Wait(syncObject);

                JObject result = response;
                response = null;

                if (IsFailedResponse(result))
                    throw new PHD2Exception((string)result["error"]["message"]);

                return result;
            }
        }

        private void Worker()
        {
            try
            {
                while (!terminate)
                {
                    string line = connection.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    Debug.WriteLine($"PHD2 Response: {line}");

                    try
                    {
                        JObject json = JObject.Parse(line);

                        if (json.ContainsKey("jsonrpc"))
                        {
                            // Response to a call
                            lock (syncObject)
                            {
                                response = json;
                                Monitor.Pulse(syncObject);
                            }
                        }
                        else
                        {
                            // Event notification
                            HandleEvent(json);
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.WriteLine($"Invalid JSON from PHD2: {ex.Message}: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PHD2 Worker thread error: {ex.Message}");
            }
        }

        private void HandleEvent(JObject eventObj)
        {
            string eventType = (string)eventObj["Event"];
            
            switch (eventType)
            {
                case "AppState":
                    AppState = (string)eventObj["State"];
                    break;

                case "Version":
                    Version = (string)eventObj["PHDVersion"];
                    PHDSubver = (string)eventObj["PHDSubver"];
                    break;

                case "StartGuiding":
                    AppState = "Guiding";
                    accumRA.Reset();
                    accumDec.Reset();
                    accumActive = true;
                    break;

                case "GuideStep":
                    if (accumActive)
                    {
                        accumRA.Add((double)eventObj["RADistanceRaw"]);
                        accumDec.Add((double)eventObj["DECDistanceRaw"]);
                        Stats = GetAccumulatedStats();
                    }
                    AppState = "Guiding";
                    AvgDist = (double)eventObj["AvgDist"];
                    break;

                case "GuidingStopped":
                    AppState = "Stopped";
                    break;

                case "Paused":
                    AppState = "Paused";
                    break;

                case "StarLost":
                    AppState = "LostLock";
                    AvgDist = (double)eventObj["AvgDist"];
                    
                    // Capture detailed star lost information
                    LastStarLost = new StarLostInfo
                    {
                        Frame = (int)eventObj["Frame"],
                        Time = (double)eventObj["Time"],
                        StarMass = (double)eventObj["StarMass"],
                        SNR = (double)eventObj["SNR"],
                        AvgDist = (double)eventObj["AvgDist"],
                        ErrorCode = (int)eventObj["ErrorCode"],
                        Status = (string)eventObj["Status"],
                        Timestamp = DateTime.Now
                    };
                    break;

                case "SettleBegin":
                    accumActive = false;
                    break;

                case "Settling":
                    var settleProgress = new SettleProgress
                    {
                        Done = false,
                        Distance = (double)eventObj["Distance"],
                        SettlePx = settlePx,
                        Time = (double)eventObj["Time"],
                        SettleTime = (double)eventObj["SettleTime"],
                        Status = 0
                    };
                    lock (syncObject)
                    {
                        settle = settleProgress;
                    }
                    break;

                case "SettleDone":
                    var doneProgress = new SettleProgress
                    {
                        Done = true,
                        Status = (int)eventObj["Status"],
                        Error = (string)eventObj["Error"]
                    };
                    lock (syncObject)
                    {
                        settle = doneProgress;
                    }
                    accumActive = true;
                    break;
            }
        }

        private GuideStats GetAccumulatedStats()
        {
            var stats = new GuideStats
            {
                RmsRA = accumRA.Stdev(),
                RmsDec = accumDec.Stdev(),
                PeakRA = accumRA.Peak(),
                PeakDec = accumDec.Peak()
            };
            stats.RmsTotal = Math.Sqrt(stats.RmsRA * stats.RmsRA + stats.RmsDec * stats.RmsDec);
            return stats;
        }

        public void Guide(double settlePixels, double settleTime, double settleTimeout)
        {
            CheckConnected();
            try
            {
                var settleParam = new JObject
                {
                    ["pixels"] = settlePixels,
                    ["time"] = settleTime,
                    ["timeout"] = settleTimeout
                };

                Call("guide", new JArray { settleParam, false }); // false = don't force calibration
                settlePx = settlePixels;
            }
            catch (Exception)
            {
                lock (syncObject)
                {
                    settle = null;
                }
                throw;
            }
        }

        public void Dither(double ditherPixels, double settlePixels, double settleTime, double settleTimeout)
        {
            CheckConnected();
            try
            {
                var settleParam = new JObject
                {
                    ["pixels"] = settlePixels,
                    ["time"] = settleTime,
                    ["timeout"] = settleTimeout
                };

                Call("dither", new JArray { ditherPixels, false, settleParam }); // false = RA only
                settlePx = settlePixels;
            }
            catch (Exception)
            {
                lock (syncObject)
                {
                    settle = null;
                }
                throw;
            }
        }

        public bool IsSettling()
        {
            CheckConnected();
            lock (syncObject)
            {
                if (settle != null)
                    return true;
            }

            // Initialize settle state
            var result = Call("get_settling");
            bool val = (bool)result["result"];

            if (val)
            {
                var settleProgress = new SettleProgress
                {
                    Done = false,
                    Distance = -1.0,
                    SettlePx = 0.0,
                    Time = 0.0,
                    SettleTime = 0.0,
                    Status = 0
                };
                lock (syncObject)
                {
                    if (settle == null)
                        settle = settleProgress;
                }
            }

            return val;
        }

        public SettleProgress CheckSettling()
        {
            CheckConnected();
            var result = new SettleProgress();

            lock (syncObject)
            {
                if (settle == null)
                    throw new PHD2Exception("Not settling");

                if (settle.Done)
                {
                    result.Done = true;
                    result.Status = settle.Status;
                    result.Error = settle.Error;
                    settle = null;
                }
                else
                {
                    result.Done = false;
                    result.Distance = settle.Distance;
                    result.SettlePx = settlePx;
                    result.Time = settle.Time;
                    result.SettleTime = settle.SettleTime;
                }
            }

            return result;
        }

        public void StopCapture(uint timeoutSeconds = 10)
        {
            CheckConnected();
            Call("stop_capture");
            // Wait for capture to stop
            Thread.Sleep(100);
        }

        public void Loop(uint timeoutSeconds = 10)
        {
            CheckConnected();
            Call("loop");
        }

        public void Pause()
        {
            CheckConnected();
            Call("set_paused", new JValue(true));
        }

        public void Unpause()
        {
            CheckConnected();
            Call("set_paused", new JValue(false));
        }

        public List<string> GetEquipmentProfiles()
        {
            CheckConnected();
            var result = Call("get_profiles");
            var profiles = new List<string>();
            
            foreach (var profile in result["result"])
            {
                profiles.Add((string)profile["name"]);
            }
            
            return profiles;
        }

        public void ConnectEquipment(string profileName)
        {
            CheckConnected();
            
            var profiles = Call("get_profiles");
            int profileId = -1;
            
            foreach (var profile in profiles["result"])
            {
                if ((string)profile["name"] == profileName)
                {
                    profileId = (int)profile["id"];
                    break;
                }
            }
            
            if (profileId == -1)
                throw new PHD2Exception($"Invalid PHD2 profile name: {profileName}");

            StopCapture();
            Call("set_connected", new JValue(false));
            Call("set_profile", new JValue(profileId));
            Call("set_connected", new JValue(true));
        }

        public double GetPixelScale()
        {
            CheckConnected();
            var result = Call("get_pixel_scale");
            return (double)result["result"];
        }

        public PHD2Status GetStatus()
        {
            return new PHD2Status
            {
                AppState = AppState,
                AvgDist = AvgDist,
                Stats = Stats?.Clone(),
                Version = Version,
                PHDSubver = PHDSubver,
                IsConnected = IsConnected,
                IsGuiding = IsGuiding(),
                IsSettling = settle != null,
                SettleProgress = settle,
                LastStarLost = LastStarLost
            };
        }

        private bool IsGuiding()
        {
            return AppState == "Guiding" || AppState == "LostLock";
        }

        private void CheckConnected()
        {
            if (!IsConnected)
                throw new PHD2Exception("PHD2 Server disconnected");
        }

        private static string MakeJsonRpc(string method, JToken param)
        {
            var request = new JObject
            {
                ["method"] = method,
                ["id"] = 1
            };

            if (param != null && param.Type != JTokenType.Null)
            {
                if (param.Type == JTokenType.Array || param.Type == JTokenType.Object)
                    request["params"] = param;
                else
                {
                    var array = new JArray { param };
                    request["params"] = array;
                }
            }

            return request.ToString(Formatting.None);
        }

        private static bool IsFailedResponse(JObject response)
        {
            return response.ContainsKey("error");
        }

        public void Dispose()
        {
            Disconnect();
            connection?.Dispose();
        }
    }
}
