using NINA.Core.Utility;
using System;
using System.IO;
using System.Linq;
using TouchNStars.Utility;

internal static class BackgroundWorker {
    private static int lastLine = 0;
    private static FileSystemWatcher watcher;

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
                if ((line.Contains("|ERROR|") || line.Contains("|WARNING|")) && line.Contains("|StartAutoFocus|")) {
                    DataContainer.afRun = false;
                    DataContainer.afError = true;

                    string[] parts = line.Split('|');
                    if (parts.Length >= 6) {
                        DataContainer.afErrorText = parts[5].Trim();
                    }
                } 
                else if (line.Contains("|BroadcastSuccessfulAutoFocusRun|")
                 || line.Contains("|Autofocus notification received"))
                {
                    DataContainer.afRun = false;
                    DataContainer.afError = false;
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
    }
}