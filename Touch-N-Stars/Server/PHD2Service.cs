using System;
using System.Threading.Tasks;
using NINA.Core.Utility;
using TouchNStars.PHD2;

namespace TouchNStars.Server
{
    public class PHD2Service : IDisposable
    {
        private PHD2Client client;
        private readonly object lockObject = new object();
        private string lastError;

        public bool IsConnected => client?.IsConnected ?? false;
        public string LastError => lastError;

        public PHD2Service()
        {
            client = new PHD2Client();
        }

        public async Task<bool> ConnectAsync(string hostname = "localhost", uint instance = 1)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        Logger.Info($"Attempting to connect to PHD2 at {hostname}, instance {instance}");
                        
                        if (client != null && client.IsConnected)
                        {
                            Logger.Info("Disconnecting existing PHD2 client");
                            client.Disconnect();
                        }

                        ushort port = (ushort)(4400 + instance - 1);
                        Logger.Info($"Creating PHD2 client for {hostname}:{port}");
                        
                        client = new PHD2Client(hostname, instance);
                        Logger.Info("Calling client.Connect()");
                        client.Connect();
                        
                        Logger.Info($"PHD2 connection successful. IsConnected: {client.IsConnected}");
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = $"Failed to connect to PHD2 at {hostname}:{4400 + instance - 1}: {ex.Message}";
                    Logger.Error($"Failed to connect to PHD2: {ex}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            });
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        client?.Disconnect();
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Error disconnecting from PHD2: {ex}");
                }
            });
        }

        public async Task<bool> StartGuidingAsync(double settlePixels = 2.0, double settleTime = 10.0, double settleTimeout = 100.0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Guide(settlePixels, settleTime, settleTimeout);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to start guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> StopGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.StopCapture();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to stop guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> DitherAsync(double ditherPixels = 3.0, double settlePixels = 2.0, double settleTime = 10.0, double settleTimeout = 100.0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Dither(ditherPixels, settlePixels, settleTime, settleTimeout);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to dither: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> PauseGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Pause();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to pause guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> UnpauseGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Unpause();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to unpause guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> StartLoopingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Loop();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to start looping: {ex}");
                    return false;
                }
            });
        }

        public async Task<System.Collections.Generic.List<string>> GetEquipmentProfilesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return new System.Collections.Generic.List<string>();
                        }

                        var profiles = client.GetEquipmentProfiles();
                        lastError = null;
                        return profiles;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get equipment profiles: {ex}");
                    return new System.Collections.Generic.List<string>();
                }
            });
        }

        public async Task<bool> ConnectEquipmentAsync(string profileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.ConnectEquipment(profileName);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to connect equipment: {ex}");
                    return false;
                }
            });
        }

        public async Task<PHD2Status> GetStatusAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            return new PHD2Status
                            {
                                IsConnected = false,
                                AppState = "Disconnected"
                            };
                        }

                        var status = client.GetStatus();
                        lastError = null;
                        return status;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get PHD2 status: {ex}");
                    return new PHD2Status
                    {
                        IsConnected = false,
                        AppState = "Error"
                    };
                }
            });
        }

        public async Task<SettleProgress> CheckSettlingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return null;
                        }

                        if (!client.IsSettling())
                        {
                            return new SettleProgress { Done = true };
                        }

                        var progress = client.CheckSettling();
                        lastError = null;
                        return progress;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to check settling: {ex}");
                    return null;
                }
            });
        }

        public async Task<double> GetPixelScaleAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return 0.0;
                        }

                        var pixelScale = client.GetPixelScale();
                        lastError = null;
                        return pixelScale;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get pixel scale: {ex}");
                    return 0.0;
                }
            });
        }

        public void Dispose()
        {
            try
            {
                lock (lockObject)
                {
                    client?.Dispose();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error disposing PHD2Service: {ex}");
            }
        }
    }
}
