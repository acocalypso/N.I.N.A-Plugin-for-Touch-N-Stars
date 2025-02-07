using Newtonsoft.Json;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.WPF.Base.Utility.AutoFocus;
using System;
using System.IO;
using System.Linq;
using TouchNStars.Utility;

internal static class BackgroundWorker {
    private static int lastLine = 0;
    private static FileSystemWatcher watcher;
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
            string[] logLines = CoreUtility.SafeRead(e.FullPath).Split('\n');

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
        }
        
        if (afWatcher != null) {
            afWatcher.EnableRaisingEvents = false;
            afWatcher.Changed -= OnAFFileChanged;
            afWatcher.Dispose();
            afWatcher = null;
        }
    }

    public static void MonitorLastAF() {
        if (afWatcher != null) return;  // Prevent multiple instances

        afWatcher = new FileSystemWatcher(CoreUtility.AfPath);
        afWatcher.EnableRaisingEvents = true; // Enable the watcher
        afWatcher.Created += OnAFFileChanged;
    }

    private static void OnAFFileChanged(object sender, FileSystemEventArgs e) {
        try {
            if (e == null || string.IsNullOrEmpty(e.FullPath)) {
                Logger.Error("Invalid file system event received");
                return;
            }

            if (e.ChangeType == WatcherChangeTypes.Created && e.FullPath.EndsWith(".json")) {
                Logger.Info("Found new AF report: " + e.FullPath);
                
                // Add retry logic for file access
                string content = null;
                for (int i = 0; i < 3; i++) {
                    try {
                        content = CoreUtility.SafeRead(e.FullPath);
                        if (!string.IsNullOrEmpty(content)) break;
                        System.Threading.Thread.Sleep(100); // Wait briefly before retry
                    }
                    catch (IOException ex) {
                        Logger.Warning($"Attempt {i + 1} to read AF file failed: {ex.Message}");
                        if (i == 2) throw; // Rethrow on final attempt
                    }
                }

                if (string.IsNullOrEmpty(content)) {
                    Logger.Error("Unable to read AF report content");
                    return;
                }

                try {
                    AutoFocusReport report = JsonConvert.DeserializeObject<AutoFocusReport>(content);
                    if (report == null) {
                        Logger.Error("Failed to deserialize AF report");
                        return;
                    }

                    if (report.Timestamp > DataContainer.lastAfTimestamp) {
                        DataContainer.lastAfTimestamp = report.Timestamp;
                        DataContainer.afRun = false;
                        DataContainer.newAfGraph = true;
                    }
                }
                catch (JsonException ex) {
                    Logger.Error($"Failed to parse AF report: {ex.Message}");
                }
            }
        }
        catch (Exception ex) {
            Logger.Error($"Error processing AF file change: {ex.Message}");
            // Don't rethrow - we want to handle all errors gracefully
        }
    }
}