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
            public string ContentType { get; set; }  // Window.Content type (language-independent)
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
        /// <param name="includeRawContent">If true, includes raw text elements for debugging</param>
        public static List<DialogInfo> GetAllDialogs(bool includeRawContent = false) {
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

                        // Get ContentType (language-independent identifier)
                        if (window.Content != null) {
                            info.ContentType = window.Content.GetType().FullName;
                        }

                        // Get DataContext if available
                        if (window.DataContext != null) {
                            var dataContextType = window.DataContext.GetType();

                            // Check if DataContext has DialogResult property
                            var dialogResultProperty = dataContextType.GetProperty("DialogResult");
                            info.HasDialogResult = dialogResultProperty != null &&
                                                 dialogResultProperty.PropertyType == typeof(bool?);

                            // Extract content from DataContext properties
                            info.Content = ExtractDialogContent(window.DataContext);
                        } else {
                            // No DataContext - extract text from window directly
                            var rawContent = ExtractTextContentFromWindow(window);

                            // If debug mode, add raw content before parsing
                            if (includeRawContent) {
                                info.Content["_RawTextElements"] = rawContent;
                            }

                            // Try to parse structured content based on ContentType
                            var parsedContent = ParseStructuredContent(info.ContentType, rawContent);

                            // Merge parsed content with raw content
                            foreach (var kvp in parsedContent) {
                                info.Content[kvp.Key] = kvp.Value;
                            }
                        }

                        // Always extract buttons from visual tree (works for all dialogs)
                        info.AvailableCommands = ExtractButtonsFromWindow(window);
                        Logger.Debug($"DialogManager: Extracted {info.AvailableCommands.Count} buttons from window '{info.Title}'");

                        // Skip dialogs without content (empty windows)
                        if (string.IsNullOrEmpty(info.ContentType) && info.Content.Count == 0 && info.AvailableCommands.Count == 0) {
                            Logger.Debug($"DialogManager: Skipping empty window '{info.Title}' (no content, no buttons)");
                            continue;
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
        /// Click a button in a window by button name or content
        /// </summary>
        /// <param name="windowTitle">Title of the window containing the button</param>
        /// <param name="buttonIdentifier">Button name or content text</param>
        /// <returns>True if button was clicked</returns>
        public static bool ClickWindowButton(string windowTitle, string buttonIdentifier) {
            bool clicked = false;

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    foreach (Window window in Application.Current.Windows) {
                        if (window == Application.Current.MainWindow) {
                            continue;
                        }

                        if (window.Title?.Contains(windowTitle) == true) {
                            Logger.Debug($"DialogManager: Searching for button '{buttonIdentifier}' in window '{window.Title}'");
                            // Search the entire window's visual tree, not just window.Content
                            clicked = TryClickButton(window, buttonIdentifier);
                            if (clicked) {
                                Logger.Info($"DialogManager: Clicked button '{buttonIdentifier}' in window '{windowTitle}'");
                                return;
                            } else {
                                Logger.Debug($"DialogManager: Button '{buttonIdentifier}' not found in window '{window.Title}'");
                            }
                        }
                    }
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error clicking window button: {ex}");
            }

            return clicked;
        }

        /// <summary>
        /// Recursively search for and click a button
        /// </summary>
        private static bool TryClickButton(System.Windows.DependencyObject obj, string buttonIdentifier) {
            if (obj == null) return false;

            // Check if it's a button
            if (obj is System.Windows.Controls.Button button) {
                var buttonName = button.Name;
                var buttonText = GetButtonText(button);  // Use GetButtonText instead of ToString()

                Logger.Debug($"DialogManager: Checking button - Name: '{buttonName}', Text: '{buttonText}'");

                // Skip PART_ buttons (internal WPF buttons) unless specifically requested
                bool isPart = buttonName?.StartsWith("PART_") == true;
                bool nameMatches = !string.IsNullOrEmpty(buttonName) && buttonName.Contains(buttonIdentifier, StringComparison.OrdinalIgnoreCase);
                bool textMatches = !string.IsNullOrEmpty(buttonText) && buttonText.Contains(buttonIdentifier, StringComparison.OrdinalIgnoreCase);

                if (nameMatches || textMatches) {
                    // Skip PART_ buttons unless the identifier explicitly targets them
                    if (isPart && !buttonIdentifier.StartsWith("PART_")) {
                        Logger.Debug($"DialogManager: Skipping PART_ button '{buttonName}'");
                    } else {
                        try {
                            Logger.Debug($"DialogManager: Attempting to click button - Name: '{buttonName}', Text: '{buttonText}'");

                            // Raise click event
                            var clickMethod = button.GetType().GetMethod("OnClick",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                            if (clickMethod != null) {
                                clickMethod.Invoke(button, null);
                                return true;
                            }

                            // Alternative: raise RoutedEvent
                            button.RaiseEvent(new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                            return true;
                        } catch (Exception ex) {
                            Logger.Error($"DialogManager: Error invoking button click: {ex}");
                        }
                    }
                }
            }

            // Search children
            try {
                var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++) {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                    if (TryClickButton(child, buttonIdentifier)) {
                        return true;
                    }
                }
            } catch {
                // Some objects don't support GetChildrenCount
            }

            return false;
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
                        // Also include Visibility enum values (for button visibility)
                        else if (value != null && prop.PropertyType.Name == "Visibility") {
                            content[prop.Name] = value.ToString();
                        }
                    } catch {
                        // Skip properties that throw exceptions when accessed
                        continue;
                    }
                }

                // Normalize: If there's a "Text" property but no "Message", copy Text to Message
                if (content.ContainsKey("Text") && !content.ContainsKey("Message")) {
                    content["Message"] = content["Text"];
                }
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error extracting content: {ex}");
            }

            return content;
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
        /// Extract button names from a window's visual tree
        /// </summary>
        private static List<string> ExtractButtonsFromWindow(Window window) {
            var buttons = new List<string>();

            try {
                Logger.Debug($"DialogManager: Extracting buttons from window '{window.Title}'");
                Logger.Debug($"DialogManager: Window.Content type: {window.Content?.GetType().Name}");

                // Try to get the visual root (the window itself is a DependencyObject)
                // Start from the window to traverse the entire visual tree
                FindButtons(window, buttons);
                Logger.Debug($"DialogManager: Found {buttons.Count} buttons");
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error extracting buttons: {ex}");
            }

            return buttons;
        }

        /// <summary>
        /// Recursively find buttons in the visual tree
        /// </summary>
        private static void FindButtons(System.Windows.DependencyObject obj, List<string> buttons) {
            if (obj == null) return;

            // Check if it's a button
            if (obj is System.Windows.Controls.Button button) {
                // Skip invisible buttons
                if (button.Visibility != System.Windows.Visibility.Visible) {
                    Logger.Debug($"DialogManager: Skipping invisible button - Name: '{button.Name}', Visibility: {button.Visibility}");
                    // Continue searching children but don't add this button
                    try {
                        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
                        for (int i = 0; i < childCount; i++) {
                            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                            FindButtons(child, buttons);
                        }
                    } catch {
                        // Some objects don't support GetChildrenCount
                    }
                    return;
                }

                var buttonName = button.Name;
                var buttonText = GetButtonText(button);

                Logger.Debug($"DialogManager: Found visible button - Name: '{buttonName}', Text: '{buttonText}'");

                if (!string.IsNullOrEmpty(buttonName) && !buttonName.StartsWith("PART_")) {
                    buttons.Add($"{buttonName}");
                } else if (!string.IsNullOrEmpty(buttonText)) {
                    buttons.Add($"{buttonText}");
                } else if (!string.IsNullOrEmpty(buttonName)) {
                    buttons.Add($"{buttonName}");
                } else {
                    buttons.Add("UnnamedButton");
                }
            }

            // Search children
            try {
                var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++) {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                    FindButtons(child, buttons);
                }
            } catch {
                // Some objects don't support GetChildrenCount
            }
        }

        /// <summary>
        /// Extract text content from a button's Content property
        /// Handles various content types like string, TextBlock, or other WPF elements
        /// </summary>
        private static string GetButtonText(System.Windows.Controls.Button button) {
            if (button == null) {
                return string.Empty;
            }

            try {
                // Check ToolTip first (often contains the button text for icon-only buttons)
                if (button.ToolTip != null) {
                    var tooltip = button.ToolTip.ToString();
                    if (!string.IsNullOrEmpty(tooltip) && tooltip != button.ToolTip.GetType().FullName) {
                        return tooltip;
                    }
                }

                if (button.Content == null) {
                    return string.Empty;
                }

                // Simple case: Content is a string
                if (button.Content is string text) {
                    return text;
                }

                // Content is a TextBlock
                if (button.Content is System.Windows.Controls.TextBlock textBlock) {
                    return textBlock.Text ?? string.Empty;
                }

                // Content is another text element
                if (button.Content is System.Windows.Controls.TextBox textBox) {
                    return textBox.Text ?? string.Empty;
                }

                if (button.Content is System.Windows.Controls.Label label) {
                    return label.Content?.ToString() ?? string.Empty;
                }

                // Content might be a complex object - try to search its visual tree for TextBlock
                if (button.Content is System.Windows.DependencyObject contentObj) {
                    var foundText = ExtractTextFromVisualTree(contentObj);
                    if (!string.IsNullOrEmpty(foundText)) {
                        return foundText;
                    }
                }

                // Fallback: ToString() on the content (but avoid type names)
                var contentStr = button.Content.ToString();
                if (contentStr != null && !contentStr.StartsWith("System.") && !contentStr.Contains(".")) {
                    return contentStr;
                }

                return string.Empty;
            } catch (Exception ex) {
                Logger.Debug($"DialogManager: Error extracting button text: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Recursively search a visual tree for text content (TextBlock, Label, etc.)
        /// </summary>
        private static string ExtractTextFromVisualTree(System.Windows.DependencyObject obj) {
            if (obj == null) return string.Empty;

            // Check if current object has text
            if (obj is System.Windows.Controls.TextBlock textBlock) {
                return textBlock.Text ?? string.Empty;
            }

            if (obj is System.Windows.Controls.Label label) {
                return label.Content?.ToString() ?? string.Empty;
            }

            if (obj is System.Windows.Controls.TextBox textBox) {
                return textBox.Text ?? string.Empty;
            }

            // Search children
            try {
                var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++) {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                    var text = ExtractTextFromVisualTree(child);
                    if (!string.IsNullOrEmpty(text)) {
                        return text;
                    }
                }
            } catch {
                // Some objects don't support GetChildrenCount
            }

            return string.Empty;
        }

        /// <summary>
        /// Extract text content from a window's visual tree (for windows without DataContext)
        /// </summary>
        private static Dictionary<string, object> ExtractTextContentFromWindow(Window window) {
            var content = new Dictionary<string, object>();

            try {
                Logger.Debug($"DialogManager: Extracting text content from window '{window.Title}'");

                var textElements = new List<string>();
                FindTextElements(window, textElements);

                // Filter out the window title from text elements
                var windowTitle = window.Title ?? "";
                var filteredElements = textElements
                    .Where(text => !text.Equals(windowTitle, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Logger.Debug($"DialogManager: Found {textElements.Count} text elements, {filteredElements.Count} after filtering title");

                // Add all text elements (excluding title)
                if (filteredElements.Count > 0) {
                    // If there's only one significant text element, use it as "Message"
                    if (filteredElements.Count == 1) {
                        content["Message"] = filteredElements[0];
                    } else {
                        // Multiple text elements - add them individually
                        for (int i = 0; i < filteredElements.Count; i++) {
                            content[$"Text{i + 1}"] = filteredElements[i];
                        }
                        // Also combine them into a single message
                        content["Message"] = string.Join(" ", filteredElements);
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error extracting text content: {ex}");
            }

            return content;
        }

        /// <summary>
        /// Recursively find text elements (TextBlock, Label) in the visual tree
        /// </summary>
        private static void FindTextElements(System.Windows.DependencyObject obj, List<string> textElements) {
            if (obj == null) return;

            // Skip buttons - we don't want button text in the message
            if (obj is System.Windows.Controls.Button) {
                return;
            }

            // Check if it's a text element
            if (obj is System.Windows.Controls.TextBlock textBlock) {
                var text = textBlock.Text;
                if (!string.IsNullOrWhiteSpace(text)) {
                    Logger.Debug($"DialogManager: Found TextBlock with text: '{text}'");
                    textElements.Add(text);
                }
            } else if (obj is System.Windows.Controls.Label label) {
                var text = label.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(text)) {
                    Logger.Debug($"DialogManager: Found Label with text: '{text}'");
                    textElements.Add(text);
                }
            } else if (obj is System.Windows.Controls.TextBox textBox) {
                var text = textBox.Text;
                if (!string.IsNullOrWhiteSpace(text)) {
                    Logger.Debug($"DialogManager: Found TextBox with text: '{text}'");
                    textElements.Add(text);
                }
            }

            // Search children
            try {
                var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < childCount; i++) {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                    FindTextElements(child, textElements);
                }
            } catch {
                // Some objects don't support GetChildrenCount
            }
        }

        /// <summary>
        /// Parse structured content based on ContentType
        /// Converts raw text extraction into meaningful structured data
        /// </summary>
        private static Dictionary<string, object> ParseStructuredContent(string contentType, Dictionary<string, object> rawContent) {
            if (string.IsNullOrEmpty(contentType) || rawContent == null || rawContent.Count == 0) {
                return rawContent ?? new Dictionary<string, object>();
            }

            try {
                // PlateSolving Status Dialog
                if (contentType.Contains("PlateSolvingStatusVM")) {
                    return ParsePlateSolvingStatus(rawContent);
                }

                // Sequencer MessageBox Result
                if (contentType.Contains("MessageBoxResult")) {
                    return ParseSequencerMessageBox(rawContent);
                }

                // Add more parsers here as needed
                // if (contentType.Contains("SomeOtherType")) { return ParseSomeOtherType(rawContent); }

            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error parsing structured content for {contentType}: {ex}");
            }

            // Fallback: return raw content
            return rawContent;
        }

        /// <summary>
        /// Parse PlateSolving status dialog into structured format
        /// </summary>
        private static Dictionary<string, object> ParsePlateSolvingStatus(Dictionary<string, object> rawContent) {
            var structured = new Dictionary<string, object> {
                ["Type"] = "PlateSolvingStatus"
            };

            int totalTexts = rawContent.Count;
            Logger.Debug($"DialogManager: Parsing PlateSolving with {totalTexts} text elements");

            // Check if this is an empty/initial state by looking for characteristic patterns
            // In empty state: Parameters are mixed with table headers
            // Empty dialogs typically have around 31 text elements (without table data)
            bool isEmpty = totalTexts <= 35; // Empty dialogs have fewer text elements

            if (isEmpty) {
                Logger.Debug($"DialogManager: Detected empty PlateSolving dialog (no table data yet)");

                // Status message is always in Text21 (fixed position for empty dialogs)
                string statusMessage = "Initializing";
                if (rawContent.TryGetValue("Text21", out var text21Obj)) {
                    var status = text21Obj?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(status)) {
                        statusMessage = status;
                        Logger.Debug($"DialogManager: Found status message in Text21: '{statusMessage}'");
                    }
                }

                // In empty state, just extract what we can without strict structure
                structured["Parameters"] = new Dictionary<string, object> {
                    ["Status"] = "Initializing"
                };
                structured["StatusMessage"] = statusMessage;
                structured["TableHeaders"] = new List<string>();
                structured["Table"] = new List<Dictionary<string, object>>();

                return structured;
            }

            // Fixed structure for PlateSolvingStatusVM with data (language-independent):
            // Text1-26: Parameters (13 key-value pairs)
            // Text27: Status message (optional, e.g., "Exposing")
            // Text28-37: Table headers (10 headers, but only 9 data columns - "Success" has no data)
            // Text38+: Table data (9 values per row)

            Logger.Debug($"DialogManager: Parsing PlateSolving with data using fixed structure");

            // Parse parameters (Text1-26 = 13 pairs)
            var parameters = new Dictionary<string, object>();
            for (int i = 1; i <= 26 && i <= totalTexts; i += 2) {
                if (rawContent.TryGetValue($"Text{i}", out var keyObj) &&
                    rawContent.TryGetValue($"Text{i + 1}", out var valueObj)) {
                    var key = keyObj?.ToString() ?? "";
                    var value = valueObj?.ToString() ?? "";

                    // Skip if the value looks like a table header (contains typical header keywords)
                    if (value.Contains("Fehler", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("Pixel", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("Zeit", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("Pix", StringComparison.OrdinalIgnoreCase)) {
                        Logger.Debug($"DialogManager: Skipping parameter pair - appears to be table header: '{key}' = '{value}'");
                        continue;
                    }

                    var cleanKey = key.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("/", "");
                    parameters[cleanKey] = value;
                }
            }

            // If we have too few valid parameters, it's likely an empty/initial state
            if (parameters.Count < 5) {
                Logger.Debug($"DialogManager: Only {parameters.Count} valid parameters found - treating as empty state");

                // Status message is in Text21 for empty dialogs
                string statusMessage = "Initializing";
                if (rawContent.TryGetValue("Text21", out var text21Obj2)) {
                    var status = text21Obj2?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(status)) {
                        statusMessage = status;
                        Logger.Debug($"DialogManager: Found status message in Text21: '{statusMessage}'");
                    }
                }

                structured["Parameters"] = new Dictionary<string, object> {
                    ["Status"] = "Initializing"
                };
                structured["StatusMessage"] = statusMessage;
                structured["TableHeaders"] = new List<string>();
                structured["Table"] = new List<Dictionary<string, object>>();
                return structured;
            }

            structured["Parameters"] = parameters;

            // Status message (Text27, optional)
            // Only include if it's a valid status message (not a table header)
            if (rawContent.TryGetValue("Text27", out var statusObj)) {
                var status = statusObj?.ToString() ?? "";
                // Filter out table headers that might appear in Text27
                // Table headers typically contain "error", "Zeit", "Time", "Pixel", "Arcsec", etc.
                var isTableHeader = status.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                   status.Contains("Pixel", StringComparison.OrdinalIgnoreCase) ||
                                   status.Contains("Arcsec", StringComparison.OrdinalIgnoreCase) ||
                                   status.Contains("Success", StringComparison.OrdinalIgnoreCase) ||
                                   status.Contains("Erfolg", StringComparison.OrdinalIgnoreCase) ||
                                   status.Contains("Fehler", StringComparison.OrdinalIgnoreCase) ||
                                   status.Equals("Zeit", StringComparison.OrdinalIgnoreCase) ||
                                   status.Equals("Time", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(status) && !isTableHeader) {
                    structured["StatusMessage"] = status;
                }
                // Don't set StatusMessage if it's a table header - will be removed later if table has data
            }

            // Table headers (Text28-37 = 10 headers)
            var headers = new List<string>();
            for (int i = 28; i <= 37 && i <= totalTexts; i++) {
                if (rawContent.TryGetValue($"Text{i}", out var headerObj)) {
                    headers.Add(headerObj?.ToString() ?? "");
                }
            }

            // Remove the 2nd header (index 1, usually "Success") as it has no corresponding data
            var dataHeaders = new List<string>();
            for (int i = 0; i < headers.Count; i++) {
                if (i != 1) { // Skip index 1 (Success column has no data)
                    dataHeaders.Add(headers[i]);
                }
            }

            structured["TableHeaders"] = dataHeaders;
            Logger.Debug($"DialogManager: Found {dataHeaders.Count} data headers (skipped header at index 1)");

            // Table data starts at Text38, 9 values per row
            var tableRows = new List<Dictionary<string, object>>();
            int dataStartIdx = 38;
            int columnsPerRow = 9;

            int rowNum = 0;
            while (dataStartIdx + (rowNum * columnsPerRow) + columnsPerRow - 1 <= totalTexts) {
                var row = new Dictionary<string, object>();
                bool hasData = false;

                for (int col = 0; col < columnsPerRow && col < dataHeaders.Count; col++) {
                    int textIdx = dataStartIdx + (rowNum * columnsPerRow) + col;
                    if (rawContent.TryGetValue($"Text{textIdx}", out var cellObj)) {
                        var cellValue = cellObj?.ToString() ?? "";
                        var header = dataHeaders[col].Replace(" ", "").Replace("(", "").Replace(")", "").Replace("/", "");
                        row[header] = cellValue;
                        hasData = true;
                    }
                }

                if (hasData) {
                    tableRows.Add(row);
                    rowNum++;
                } else {
                    break;
                }
            }

            Logger.Debug($"DialogManager: Extracted {tableRows.Count} table rows");

            structured["Table"] = tableRows;

            if (tableRows.Count > 0) {
                structured["LatestResult"] = tableRows[0]; // First row is most recent

                // When table has data, the process is complete
                // Check if StatusMessage is a table header and replace it
                if (structured.ContainsKey("StatusMessage")) {
                    var statusMsg = structured["StatusMessage"]?.ToString() ?? "";
                    var isTableHeader = statusMsg.Equals("Zeit", StringComparison.OrdinalIgnoreCase) ||
                                       statusMsg.Equals("Time", StringComparison.OrdinalIgnoreCase) ||
                                       statusMsg.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                       statusMsg.Contains("Fehler", StringComparison.OrdinalIgnoreCase) ||
                                       statusMsg.Contains("Pixel", StringComparison.OrdinalIgnoreCase);

                    if (isTableHeader) {
                        // Remove StatusMessage when process is complete (table has data)
                        structured.Remove("StatusMessage");
                    }
                }
            }

            return structured;
        }

        /// <summary>
        /// Parse Sequencer MessageBox into structured format
        /// </summary>
        private static Dictionary<string, object> ParseSequencerMessageBox(Dictionary<string, object> rawContent) {
            var structured = new Dictionary<string, object> {
                ["Type"] = "SequencerMessage"
            };

            // Extract message text
            if (rawContent.TryGetValue("Message", out var message)) {
                structured["Message"] = message;
            }

            return structured;
        }

        /// <summary>
        /// Get detailed debug information about all windows and their properties
        /// </summary>
        public static List<Dictionary<string, object>> GetDetailedWindowInfo() {
            var windowInfoList = new List<Dictionary<string, object>>();

            try {
                Application.Current?.Dispatcher.Invoke(() => {
                    foreach (Window window in Application.Current.Windows) {
                        var windowInfo = new Dictionary<string, object>();

                        try {
                            // Basic Window properties
                            windowInfo["WindowType"] = window.GetType().FullName;
                            windowInfo["WindowTypeName"] = window.GetType().Name;
                            windowInfo["Title"] = window.Title ?? "";
                            windowInfo["IsMainWindow"] = window == Application.Current.MainWindow;
                            windowInfo["IsCustomWindow"] = window is CustomWindow;
                            windowInfo["WindowState"] = window.WindowState.ToString();
                            windowInfo["IsActive"] = window.IsActive;
                            windowInfo["IsLoaded"] = window.IsLoaded;
                            windowInfo["IsVisible"] = window.IsVisible;

                            // Window Content information (important for identifying dialogs!)
                            if (window.Content != null) {
                                var contentType = window.Content.GetType();
                                windowInfo["ContentType"] = contentType.FullName;
                                windowInfo["ContentTypeName"] = contentType.Name;
                                windowInfo["ContentNamespace"] = contentType.Namespace;
                            } else {
                                windowInfo["ContentType"] = null;
                            }

                            // DataContext information
                            if (window.DataContext != null) {
                                var dcType = window.DataContext.GetType();
                                windowInfo["DataContextType"] = dcType.FullName;
                                windowInfo["DataContextTypeName"] = dcType.Name;
                                windowInfo["DataContextNamespace"] = dcType.Namespace;

                                // All public properties of DataContext
                                var dcProperties = new Dictionary<string, object>();
                                foreach (var prop in dcType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                                    try {
                                        var value = prop.GetValue(window.DataContext);
                                        dcProperties[prop.Name] = new {
                                            Type = prop.PropertyType.Name,
                                            Value = value?.ToString() ?? "null",
                                            CanRead = prop.CanRead,
                                            CanWrite = prop.CanWrite
                                        };
                                    } catch (Exception ex) {
                                        dcProperties[prop.Name] = $"Error: {ex.Message}";
                                    }
                                }
                                windowInfo["DataContextProperties"] = dcProperties;
                            } else {
                                windowInfo["DataContextType"] = null;
                            }

                            // Window's own properties
                            var windowProperties = new Dictionary<string, object>();
                            foreach (var prop in window.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                                try {
                                    // Skip complex properties that might cause issues
                                    if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) {
                                        var value = prop.GetValue(window);
                                        windowProperties[prop.Name] = value?.ToString() ?? "null";
                                    }
                                } catch {
                                    // Skip properties that throw exceptions
                                }
                            }
                            windowInfo["WindowProperties"] = windowProperties;

                        } catch (Exception ex) {
                            windowInfo["Error"] = ex.Message;
                        }

                        windowInfoList.Add(windowInfo);
                    }
                });
            } catch (Exception ex) {
                Logger.Error($"DialogManager: Error getting detailed window info: {ex}");
            }

            return windowInfoList;
        }
    }
}
