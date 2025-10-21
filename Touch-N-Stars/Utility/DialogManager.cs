using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace TouchNStars.Utility {
    /// <summary>
    /// Manager for detecting and controlling NINA dialogs
    /// </summary>
    public static class DialogManager {

        /// <summary>
        /// Information about an active dialog window
        /// </summary>
        public class DialogInfo {
            public string WindowType { get; set; }
            public string DataContextType { get; set; }
            public string Title { get; set; }
            public bool IsCustomWindow { get; set; }
            public bool HasDialogResult { get; set; }
            public int WindowHashCode { get; set; }
            public DateTime DetectedAt { get; set; }
        }

        /// <summary>
        /// Get all currently active dialog windows (excluding main window)
        /// </summary>
        public static List<DialogInfo> GetAllDialogs() {
            var dialogs = new List<DialogInfo>();

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    foreach (Window window in Application.Current.Windows) {
                        // Skip main window
                        if (window == Application.Current.MainWindow) {
                            continue;
                        }

                        var info = new DialogInfo {
                            WindowType = window.GetType().FullName,
                            Title = window.Title ?? "",
                            IsCustomWindow = window is CustomWindow,
                            WindowHashCode = window.GetHashCode(),
                            DetectedAt = DateTime.Now
                        };

                        // Get DataContext type if available
                        if (window.DataContext != null) {
                            info.DataContextType = window.DataContext.GetType().FullName;

                            // Check if DataContext has DialogResult property
                            var dialogResultProperty = window.DataContext.GetType().GetProperty("DialogResult");
                            info.HasDialogResult = dialogResultProperty != null &&
                                                 dialogResultProperty.PropertyType == typeof(bool?);
                        }

                        dialogs.Add(info);
                    }
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error getting dialogs: {ex}");
            }

            return dialogs;
        }

        /// <summary>
        /// Close all dialog windows (excluding main window)
        /// </summary>
        /// <param name="confirmResult">Set to true to "confirm/accept" dialogs, false to "cancel" them</param>
        /// <returns>Number of dialogs closed</returns>
        public static int CloseAllDialogs(bool confirmResult = true) {
            int closedCount = 0;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    var dialogsToClose = new List<Window>();

                    // Collect all dialogs first
                    foreach (Window window in Application.Current.Windows) {
                        if (window != Application.Current.MainWindow) {
                            dialogsToClose.Add(window);
                        }
                    }

                    // Close each dialog
                    foreach (var window in dialogsToClose) {
                        try {
                            if (CloseDialog(window, confirmResult)) {
                                closedCount++;
                            }
                        } catch (Exception ex) {
                            Logger.Error($"DialogManager: Error closing dialog {window.GetType().Name}: {ex}");
                        }
                    }
                });

                Logger.Info($"DialogManager: Closed {closedCount} dialog(s) with result: {confirmResult}");
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error in CloseAllDialogs: {ex}");
            }

            return closedCount;
        }

        /// <summary>
        /// Close dialogs of a specific type
        /// </summary>
        /// <param name="dataContextTypeName">Full or partial name of the DataContext type (e.g., "FramingPlateSolvePromptVM")</param>
        /// <param name="confirmResult">Set to true to "confirm/accept" dialogs, false to "cancel" them</param>
        /// <returns>Number of dialogs closed</returns>
        public static int CloseDialogsByType(string dataContextTypeName, bool confirmResult = true) {
            int closedCount = 0;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    var dialogsToClose = new List<Window>();

                    // Collect matching dialogs
                    foreach (Window window in Application.Current.Windows) {
                        if (window != Application.Current.MainWindow && window.DataContext != null) {
                            var contextType = window.DataContext.GetType().FullName;
                            if (contextType != null && contextType.Contains(dataContextTypeName)) {
                                dialogsToClose.Add(window);
                            }
                        }
                    }

                    // Close each matching dialog
                    foreach (var window in dialogsToClose) {
                        try {
                            if (CloseDialog(window, confirmResult)) {
                                closedCount++;
                            }
                        } catch (Exception ex) {
                            Logger.Error($"DialogManager: Error closing dialog {window.GetType().Name}: {ex}");
                        }
                    }
                });

                Logger.Info($"DialogManager: Closed {closedCount} dialog(s) of type '{dataContextTypeName}' with result: {confirmResult}");
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error in CloseDialogsByType: {ex}");
            }

            return closedCount;
        }

        /// <summary>
        /// Close a specific dialog window
        /// </summary>
        /// <param name="window">The window to close</param>
        /// <param name="confirmResult">Set to true to "confirm/accept", false to "cancel"</param>
        /// <returns>True if successfully closed</returns>
        private static bool CloseDialog(Window window, bool confirmResult) {
            try {
                // Try MVVM approach first: Set DialogResult in DataContext
                if (window.DataContext != null) {
                    var dialogResultProperty = window.DataContext.GetType().GetProperty("DialogResult");

                    if (dialogResultProperty != null && dialogResultProperty.PropertyType == typeof(bool?)) {
                        Logger.Debug($"DialogManager: Setting DialogResult={confirmResult} on {window.DataContext.GetType().Name}");
                        dialogResultProperty.SetValue(window.DataContext, confirmResult);

                        // The DialogCloser attached behavior should close the window automatically
                        // If not, we'll force close below
                        return true;
                    }
                }

                // Fallback: Try to set Window.DialogResult (only works for modal dialogs)
                if (window.IsLoaded) {
                    try {
                        window.DialogResult = confirmResult;
                        Logger.Debug($"DialogManager: Set Window.DialogResult={confirmResult} on {window.GetType().Name}");
                        return true;
                    } catch {
                        // DialogResult can only be set on modal windows
                        // For non-modal windows, just close
                    }
                }

                // Last resort: Force close
                window.Close();
                Logger.Debug($"DialogManager: Force closed {window.GetType().Name}");
                return true;

            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error closing window: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Execute a command on a dialog's DataContext by command name
        /// Useful for "Accept", "Solve", "Continue" type commands
        /// </summary>
        /// <param name="dataContextTypeName">Type name of the dialog to target</param>
        /// <param name="commandName">Name of the command property (e.g., "SolveCommand", "ContinueCommand")</param>
        /// <returns>Number of commands executed</returns>
        public static int ExecuteDialogCommand(string dataContextTypeName, string commandName) {
            int executedCount = 0;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    foreach (Window window in Application.Current.Windows) {
                        if (window != Application.Current.MainWindow && window.DataContext != null) {
                            var contextType = window.DataContext.GetType();

                            if (contextType.FullName.Contains(dataContextTypeName)) {
                                var commandProperty = contextType.GetProperty(commandName);

                                if (commandProperty != null && typeof(System.Windows.Input.ICommand).IsAssignableFrom(commandProperty.PropertyType)) {
                                    var command = commandProperty.GetValue(window.DataContext) as System.Windows.Input.ICommand;

                                    if (command?.CanExecute(null) == true) {
                                        command.Execute(null);
                                        executedCount++;
                                        Logger.Info($"DialogManager: Executed {commandName} on {contextType.Name}");
                                    }
                                }
                            }
                        }
                    }
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error executing command: {ex}");
            }

            return executedCount;
        }

        /// <summary>
        /// Get count of active dialogs
        /// </summary>
        public static int GetDialogCount() {
            int count = 0;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    count = Application.Current.Windows.Cast<Window>()
                        .Count(w => w != Application.Current.MainWindow);
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error getting dialog count: {ex}");
            }

            return count;
        }
    }
}
