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
            public string DataContextTypeName { get; set; }  // Kurzer Name ohne Namespace
            public string Category { get; set; }  // z.B. "Equipment", "Platesolving", "MessageBox"
            public string SubCategory { get; set; }  // z.B. "FilterWheelChange" für spezifische MessageBox-Typen
            public string Title { get; set; }
            public bool IsCustomWindow { get; set; }
            public bool HasDialogResult { get; set; }
            public int WindowHashCode { get; set; }
            public DateTime DetectedAt { get; set; }
            public Dictionary<string, object> Content { get; set; }
            public List<string> AvailableCommands { get; set; }
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

                        // Skip toast notifications (they're not real dialogs)
                        var windowTypeName = window.GetType().FullName ?? "";
                        if (windowTypeName.Contains("ToastNotifications") ||
                            windowTypeName.Contains("NotificationsWindow")) {
                            continue;
                        }

                        var info = new DialogInfo {
                            WindowType = window.GetType().FullName,
                            Title = window.Title ?? "",
                            IsCustomWindow = window is CustomWindow,
                            WindowHashCode = window.GetHashCode(),
                            DetectedAt = DateTime.Now,
                            Content = new Dictionary<string, object>(),
                            AvailableCommands = new List<string>()
                        };

                        // Get DataContext type if available
                        if (window.DataContext != null) {
                            var dataContextType = window.DataContext.GetType();
                            info.DataContextType = dataContextType.FullName;
                            info.DataContextTypeName = dataContextType.Name;
                            info.Category = DetermineDialogCategory(dataContextType.FullName);

                            // Check if DataContext has DialogResult property
                            var dialogResultProperty = dataContextType.GetProperty("DialogResult");
                            info.HasDialogResult = dialogResultProperty != null &&
                                                 dialogResultProperty.PropertyType == typeof(bool?);

                            // Extract content from DataContext properties
                            info.Content = ExtractDialogContent(window.DataContext);

                            // Extract available commands
                            info.AvailableCommands = ExtractCommands(window.DataContext);

                            // Determine sub-category based on content (for MessageBoxes)
                            info.SubCategory = DetermineMessageBoxSubCategory(info.Content, info.Title);
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

        /// <summary>
        /// Click a specific button on NINA MessageBoxes by type (Yes/No/OK/Cancel)
        /// </summary>
        /// <param name="buttonType">"Yes", "No", "OK", or "Cancel"</param>
        /// <returns>Number of buttons clicked</returns>
        public static int ClickMessageBoxButton(string buttonType) {
            int clickedCount = 0;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    foreach (Window window in Application.Current.Windows) {
                        if (window == Application.Current.MainWindow) {
                            continue;
                        }

                        // Check if it's a NINA MessageBox
                        if (window.DataContext?.GetType().Name == "MyMessageBox") {
                            bool shouldClick = false;
                            bool dialogResult = true;

                            // Determine which button to click based on visibility
                            var visibilityProps = window.DataContext.GetType().GetProperties()
                                .Where(p => p.Name.EndsWith("Visibility"))
                                .ToDictionary(p => p.Name, p => p.GetValue(window.DataContext));

                            switch (buttonType.ToUpperInvariant()) {
                                case "YES":
                                    if (IsButtonVisible(visibilityProps, "YesVisibility")) {
                                        shouldClick = true;
                                        dialogResult = true;
                                    }
                                    break;
                                case "NO":
                                    if (IsButtonVisible(visibilityProps, "NoVisibility")) {
                                        shouldClick = true;
                                        dialogResult = false;
                                    }
                                    break;
                                case "OK":
                                    if (IsButtonVisible(visibilityProps, "OKVisibility")) {
                                        shouldClick = true;
                                        dialogResult = true;
                                    }
                                    break;
                                case "CANCEL":
                                    if (IsButtonVisible(visibilityProps, "CancelVisibility")) {
                                        shouldClick = true;
                                        dialogResult = false;
                                    }
                                    break;
                            }

                            if (shouldClick) {
                                var dialogResultProp = window.DataContext.GetType().GetProperty("DialogResult");
                                if (dialogResultProp != null) {
                                    dialogResultProp.SetValue(window.DataContext, dialogResult);
                                    clickedCount++;
                                    Logger.Info($"DialogManager: Clicked '{buttonType}' button on MessageBox");
                                }
                            }
                        }
                    }
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error clicking button: {ex}");
            }

            return clickedCount;
        }

        /// <summary>
        /// Helper to check if a button is visible
        /// </summary>
        private static bool IsButtonVisible(Dictionary<string, object> visibilityProps, string propertyName) {
            if (visibilityProps.TryGetValue(propertyName, out var value)) {
                // Visibility.Visible = 0, Collapsed/Hidden = 1/2
                if (value is int intValue) {
                    return intValue == 0;
                }
                // Could also be System.Windows.Visibility enum
                return value?.ToString() == "Visible";
            }
            return false;
        }

        /// <summary>
        /// Extract readable content from a dialog's DataContext
        /// </summary>
        private static Dictionary<string, object> ExtractDialogContent(object dataContext) {
            var content = new Dictionary<string, object>();

            if (dataContext == null) {
                return content;
            }

            try {
                var type = dataContext.GetType();
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties) {
                    try {
                        // Skip certain types that are not useful for content display
                        if (prop.PropertyType.IsAssignableFrom(typeof(System.Windows.Input.ICommand)) ||
                            prop.PropertyType.FullName?.Contains("Command") == true) {
                            continue; // Skip commands, we handle them separately
                        }

                        // Skip complex objects that might cause serialization issues
                        if (prop.PropertyType.IsClass &&
                            prop.PropertyType != typeof(string) &&
                            !prop.PropertyType.IsPrimitive &&
                            prop.PropertyType.Namespace?.StartsWith("NINA") == true) {
                            continue;
                        }

                        var value = prop.GetValue(dataContext);

                        // Only include simple, serializable types
                        if (value != null && IsSerializableType(value)) {
                            content[prop.Name] = value;
                        }
                    } catch {
                        // Skip properties that throw exceptions when accessed
                        continue;
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error extracting content: {ex}");
            }

            return content;
        }

        /// <summary>
        /// Extract available ICommand properties from DataContext
        /// </summary>
        private static List<string> ExtractCommands(object dataContext) {
            var commands = new List<string>();

            if (dataContext == null) {
                return commands;
            }

            try {
                var type = dataContext.GetType();
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties) {
                    try {
                        if (typeof(System.Windows.Input.ICommand).IsAssignableFrom(prop.PropertyType)) {
                            var command = prop.GetValue(dataContext) as System.Windows.Input.ICommand;
                            if (command != null && command.CanExecute(null)) {
                                commands.Add(prop.Name);
                            }
                        }
                    } catch {
                        // Skip properties that throw exceptions
                        continue;
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error extracting commands: {ex}");
            }

            return commands;
        }

        /// <summary>
        /// Check if a value is of a simple, serializable type
        /// </summary>
        private static bool IsSerializableType(object value) {
            if (value == null) return false;

            var type = value.GetType();

            // Allow primitives, strings, DateTime, TimeSpan, Guid
            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type == typeof(decimal)) {
                return true;
            }

            // Allow enums
            if (type.IsEnum) {
                return true;
            }

            // Allow nullable versions of above
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determine the category of a dialog based on its DataContext type
        /// This provides a language-independent way to identify dialog types
        /// </summary>
        private static string DetermineDialogCategory(string dataContextTypeName) {
            if (string.IsNullOrEmpty(dataContextTypeName)) {
                return "Unknown";
            }

            var typeLower = dataContextTypeName.ToLowerInvariant();

            // Equipment dialogs
            if (typeLower.Contains("filterwheel") || typeLower.Contains("filter")) {
                return "FilterWheel";
            }
            if (typeLower.Contains("camera")) {
                return "Camera";
            }
            if (typeLower.Contains("telescope") || typeLower.Contains("mount")) {
                return "Mount";
            }
            if (typeLower.Contains("focuser")) {
                return "Focuser";
            }
            if (typeLower.Contains("rotator")) {
                return "Rotator";
            }
            if (typeLower.Contains("dome")) {
                return "Dome";
            }
            if (typeLower.Contains("flatdevice") || typeLower.Contains("flatpanel")) {
                return "FlatDevice";
            }
            if (typeLower.Contains("guider") || typeLower.Contains("phd2")) {
                return "Guider";
            }
            if (typeLower.Contains("switch")) {
                return "Switch";
            }
            if (typeLower.Contains("safetymonitor")) {
                return "SafetyMonitor";
            }
            if (typeLower.Contains("weather")) {
                return "Weather";
            }

            // Imaging & Platesolving
            if (typeLower.Contains("platesolv")) {
                return "PlateSolving";
            }
            if (typeLower.Contains("framing")) {
                return "Framing";
            }
            if (typeLower.Contains("autofocus")) {
                return "AutoFocus";
            }
            if (typeLower.Contains("meridianflip")) {
                return "MeridianFlip";
            }
            if (typeLower.Contains("dithering")) {
                return "Dithering";
            }

            // Sequence & Control
            if (typeLower.Contains("sequence")) {
                return "Sequence";
            }
            if (typeLower.Contains("messagebox")) {
                return "MessageBox";
            }
            if (typeLower.Contains("prompt")) {
                return "Prompt";
            }

            // Settings & Configuration
            if (typeLower.Contains("settings") || typeLower.Contains("options")) {
                return "Settings";
            }
            if (typeLower.Contains("profile")) {
                return "Profile";
            }

            // Updates & Notifications
            if (typeLower.Contains("version") || typeLower.Contains("update")) {
                return "Update";
            }

            // TouchNStars specific
            if (typeLower.Contains("touchnstars") || typeLower.Contains("tns")) {
                return "TouchNStars";
            }

            // Generic NINA dialogs
            if (typeLower.Contains("nina")) {
                return "NINA";
            }

            return "Other";
        }

        /// <summary>
        /// Determine the specific type of MessageBox based on content analysis
        /// This provides language-independent identification of MessageBox purpose
        /// </summary>
        private static string DetermineMessageBoxSubCategory(Dictionary<string, object> content, string title) {
            if (content == null || content.Count == 0) {
                return null;
            }

            // Get text content
            string text = content.TryGetValue("Text", out var textValue) ? textValue?.ToString()?.ToLowerInvariant() : "";
            string titleLower = title?.ToLowerInvariant() ?? "";

            // Filter wheel change detection
            // German: "Bitte zu Filter", "Filterwechsel"
            // English: "Please change to filter", "Filter change required"
            if ((text != null && (text.Contains("filter") && (text.Contains("wechsel") || text.Contains("change to filter")))) ||
                (titleLower.Contains("filter") && (titleLower.Contains("wechsel") || titleLower.Contains("change required")))) {
                return "FilterWheelChange";
            }

            // Equipment disconnect warnings
            if (titleLower.Contains("disconnect") || titleLower.Contains("trennen")) {
                return "EquipmentDisconnect";
            }

            // Meridian flip
            if (titleLower.Contains("meridian") || text.Contains("meridian")) {
                return "MeridianFlip";
            }

            // Autofocus
            if (titleLower.Contains("focus") || text.Contains("focus") || text.Contains("fokus")) {
                return "AutoFocus";
            }

            // Plate solving
            if (titleLower.Contains("plate") || titleLower.Contains("solve") || text.Contains("plate") || text.Contains("solve")) {
                return "PlateSolve";
            }

            // Sequence warnings/errors
            if (titleLower.Contains("sequence") || titleLower.Contains("sequenz")) {
                return "Sequence";
            }

            // File/Save operations
            if (titleLower.Contains("save") || titleLower.Contains("speichern") || titleLower.Contains("file") || titleLower.Contains("datei")) {
                return "FileSave";
            }

            // Confirmation dialogs
            if (text.Contains("are you sure") || text.Contains("sind sie sicher") || text.Contains("confirm") || text.Contains("bestätigen")) {
                return "Confirmation";
            }

            // Error messages
            if (titleLower.Contains("error") || titleLower.Contains("fehler") || titleLower.Contains("warning") || titleLower.Contains("warnung")) {
                return "Error";
            }

            return null;
        }
    }
}
