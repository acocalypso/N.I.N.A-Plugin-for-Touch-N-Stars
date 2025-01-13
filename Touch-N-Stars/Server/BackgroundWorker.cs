using Newtonsoft.Json;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.WPF.Base.Utility.AutoFocus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TouchNStars.Server;
using TouchNStars.Utility;

internal static class BackgroundWorker {
    private static int lastLine = 0;
    private static FileSystemWatcher watcher;  // Add static field
    private static FileSystemWatcher afWatcher;

    public static void MonitorLogForEvents() {
        if (watcher != null) return;  // Prevent multiple instances

        try {
            string currentLogFile = Directory.GetFiles(CoreUtility.LogPath).OrderByDescending(File.GetCreationTime).First();

            watcher = new FileSystemWatcher(CoreUtility.LogPath, Path.GetFileName(currentLogFile));
            watcher.EnableRaisingEvents = true;  // Enable the watcher
            watcher.Changed += OnLogFileChanged;
        } catch (Exception ex) {
            Logger.Error($"Failed to start log monitoring: {ex.Message}");
        }
    }

    private static void OnLogFileChanged(object sender, FileSystemEventArgs e) {
        try {
            string[] logLines = [];

            using (var stream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream)) {
                string content = reader.ReadToEnd();
                logLines = content.Split('\n');
            }

            string[] newLines = logLines.Skip(lastLine).ToArray();
            lastLine = logLines.Length;

            foreach (string line in newLines) {
                if (line.Contains("WebServerTask") && line.Contains("http://localhost:") && line.Contains("/dist")) {
                    string[] partsAfterLocalhost = line.Split("http://localhost:");
                    string[] partsAfterPort = partsAfterLocalhost[1].Split('/');
                    string[] partsAfterDist = partsAfterPort[0].Split(':');
                    int port = int.Parse(partsAfterDist[0]);
                    DataContainer.wshvPort = port;
                    DataContainer.wshvActive = true;
                } else if ((line.Contains("|ERROR|") || line.Contains("|WARNING|")) && line.Contains("|StartAutoFocus|")) {
                    DataContainer.afRun = false;
                    DataContainer.afError = true;

                    string[] parts = line.Split('|');
                    if (parts.Length >= 6) {
                        DataContainer.afErrorText = parts[5].Trim();
                    }
                }
            }
        } catch (Exception ex) {
            Logger.Error($"Error processing log file: {ex.Message}");
        }
    }

    public static void Cleanup() {
        if (watcher != null) {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnLogFileChanged;
            watcher.Dispose();
            watcher = null;
            afWatcher.EnableRaisingEvents = false;
            afWatcher.Changed -= OnAFFileChanged;
            afWatcher.Dispose();
            afWatcher = null;
            TouchNStars.TouchNStars.Mediators.Guider.GuideEvent -= Guider_GuideEvent;
        }
    }

    public static void ObserveGuider() {
        TouchNStars.TouchNStars.Mediators.Guider.GuideEvent += Guider_GuideEvent;
    }

    private static void Guider_GuideEvent(object sender, IGuideStep e) {
        double raDistance = Math.Round((double)e.RADistanceRaw * (double)TouchNStars.TouchNStars.Mediators.Guider.GetInfo().PixelScale, 2);
        double decDistance = Math.Round((double)e.DECDistanceRaw * (double)TouchNStars.TouchNStars.Mediators.Guider.GetInfo().PixelScale, 2);

        lock (DataContainer.lockObj) {
            DataContainer.guiderData.AddValues(raDistance, decDistance);
        }
    }

    public static void MonitorLastAF() {
        if (afWatcher != null) return;  // Prevent multiple instances

        afWatcher = new FileSystemWatcher(CoreUtility.AfPath, "*.json");
        afWatcher.EnableRaisingEvents = true;  // Enable the watcher
        afWatcher.Changed += OnAFFileChanged;
    }

    private static void OnAFFileChanged(object sender, FileSystemEventArgs e) {
        if (e.ChangeType == WatcherChangeTypes.Changed) {
            AutoFocusReport report = JsonConvert.DeserializeObject<AutoFocusReport>(File.ReadAllText(e.FullPath));
            if (report.Timestamp > DataContainer.lastAfTimestamp) {
                DataContainer.lastAfTimestamp = report.Timestamp;
                DataContainer.afRun = false;
                DataContainer.newAfGraph = true;
            }
        }
    }
}