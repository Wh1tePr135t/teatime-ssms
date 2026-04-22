# SSMS Integration Guide

## Connecting Your Extension to the SSMS Editor

This guide explains how to integrate your IntelliSense extension with the SSMS editor so it actually appears when users are typing SQL.

## Key SSMS APIs and Interfaces

### IObjectExplorerService
Provides access to the Object Explorer in SSMS, allowing you to:
- Get current database context
- Navigate database objects
- Monitor schema changes

```csharp
// Example usage
var objectExplorer = GetObjectExplorerService();
var currentDatabase = objectExplorer.GetCurrentDatabase();
var tables = objectExplorer.GetTablesInDatabase(currentDatabase);
```

### ITextBuffer Events
Monitor text changes in the editor:

```csharp
// Hook into editor events
textBuffer.Changed += TextBuffer_Changed;
textBuffer.PostChanged += TextBuffer_PostChanged;

private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
{
    // Get the current position
    var position = textBuffer.CurrentPosition;
    var query = textBuffer.GetContent();
    
    // Request completions
    var suggestions = _intelliSenseProvider.GetCompletions(query, position, currentDatabase);
    
    // Show completions popup
    ShowCompletionPopup(suggestions);
}
```

## Implementation Steps

### Step 1: Create the Editor Extension Class

```csharp
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System.Collections.Generic;

namespace TEATime.Editor
{
    public class SqlEditorExtension : IWpfTextViewCreationListener
    {
        private IntelliSenseProvider _intelliSenseProvider;
        private CompletionPopup _completionPopup;

        public void TextViewCreated(IWpfTextView textView)
        {
            // Initialize the extension for this editor instance
            _intelliSenseProvider = new IntelliSenseProvider();

            // Hook into text buffer events
            textView.TextBuffer.Changed += (sender, e) => OnTextChanged(textView, e);
            textView.Caret.PositionChanged += (sender, e) => OnCaretPositionChanged(textView);
        }

        private void OnTextChanged(IWpfTextView textView, TextContentChangedEventArgs e)
        {
            // Handle text changes (user typing, pasting, etc.)
        }

        private void OnCaretPositionChanged(IWpfTextView textView)
        {
            // Handle caret position changes
            var position = textView.Caret.Position.BufferPosition.Position;
            var query = textView.TextBuffer.CurrentSnapshot.GetText();
            var database = GetCurrentDatabase();

            // Get suggestions
            var suggestions = _intelliSenseProvider.GetCompletions(query, position, database);

            // Show popup
            if (suggestions.Count > 0)
            {
                _completionPopup?.Show(textView, suggestions);
            }
            else
            {
                _completionPopup?.Hide();
            }
        }

        private string GetCurrentDatabase()
        {
            // Get from Object Explorer or connection info
            return "master"; // Default for MVP
        }
    }
}
```

### Step 2: Create the Completion Popup UI

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace TEATime.Editor
{
    public class CompletionPopup : UserControl
    {
        private ListBox _suggestionListBox;
        private Popup _popup;

        public CompletionPopup()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create the popup
            _popup = new Popup();

            // Create the list box for suggestions
            _suggestionListBox = new ListBox
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                MaxHeight = 300,
                MinWidth = 200
            };

            _suggestionListBox.DoubleClick += (s, e) =>
            {
                if (_suggestionListBox.SelectedItem is CompletionSuggestion suggestion)
                {
                    InsertSuggestion(suggestion);
                    Hide();
                }
            };

            _popup.Child = _suggestionListBox;
        }

        public void Show(IWpfTextView textView, List<CompletionSuggestion> suggestions)
        {
            // Bind suggestions to the list box
            _suggestionListBox.ItemsSource = suggestions;

            // Position the popup at the current cursor position
            var caretPoint = textView.Caret.Position.BufferPosition;
            var coordinates = textView.TextViewLines.GetCharacterBounds(caretPoint);

            _popup.HorizontalOffset = coordinates.Left;
            _popup.VerticalOffset = coordinates.Bottom;
            _popup.IsOpen = true;

            // Set focus to the list box for keyboard navigation
            _suggestionListBox.Focus();
            if (_suggestionListBox.Items.Count > 0)
            {
                _suggestionListBox.SelectedIndex = 0;
            }
        }

        public void Hide()
        {
            _popup.IsOpen = false;
        }

        private void InsertSuggestion(CompletionSuggestion suggestion)
        {
            // Insert the suggestion text at the cursor position
            // This would interact with the text buffer
        }
    }
}
```

### Step 3: Register Event Handlers

In `TEATimePackage.cs`:

```csharp
public class TEATimePackage : IVsPackage
{
    private IVsTextManager _textManager;
    private IVsRunningDocumentTable _runningDocTable;

    public int Initialize()
    {
        try
        {
            // Get SSMS services
            _textManager = GetService(typeof(IVsTextManager)) as IVsTextManager;
            _runningDocTable = GetService(typeof(IVsRunningDocumentTable)) as IVsRunningDocumentTable;

            // Register event handlers
            if (_runningDocTable != null)
            {
                _runningDocTable.AdviseRunningDocTableEvents(this);
            }

            _intelliSenseProvider = new IntelliSenseProvider();

            return 0; // S_OK
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            return 1; // E_FAIL
        }
    }
}
```

## Keyboard Shortcuts

Add keyboard bindings for snippet expansion:

```csharp
// In the editor extension
textView.KeyDown += (sender, e) =>
{
    if (e.Key == System.Windows.Input.Key.Tab)
    {
        // Handle Tab key for snippet expansion
        if (_suggestionListBox.SelectedItem is CompletionSuggestion suggestion)
        {
            InsertSuggestion(suggestion);
            e.Handled = true;
        }
    }
    else if (e.Key == System.Windows.Input.Key.Escape)
    {
        // Hide completion popup
        _completionPopup?.Hide();
        e.Handled = true;
    }
};
```

## Database Context Detection

Get the current database from SSMS:

```csharp
public class DatabaseContextProvider
{
    private IObjectExplorerService _objectExplorer;

    public string GetCurrentDatabase()
    {
        try
        {
            // Method 1: From Object Explorer
            var selectedNode = _objectExplorer.GetSelectedNode();
            if (selectedNode?.NodeType == "Database")
            {
                return selectedNode.NodeName;
            }

            // Method 2: From connection info
            var connection = _objectExplorer.GetConnectionInfo();
            return connection?.Database ?? "master";
        }
        catch
        {
            return "master"; // Default fallback
        }
    }

    public List<string> GetAvailableDatabases()
    {
        try
        {
            var connection = _objectExplorer.GetConnectionInfo();
            var databases = new List<string>();

            using (var sqlConnection = new SqlConnection(connection.ConnectionString))
            {
                sqlConnection.Open();
                var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", sqlConnection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        databases.Add(reader["name"].ToString());
                    }
                }
            }

            return databases;
        }
        catch
        {
            return new List<string> { "master" };
        }
    }
}
```

## Handling Large Queries

For optimal performance with large queries:

```csharp
public class QueryOptimizer
{
    public static string GetRelevantContext(string query, int cursorPosition, int contextLines = 5)
    {
        var lines = query.Split('\n');
        var currentLine = GetLineNumber(query, cursorPosition);
        
        var startLine = Math.Max(0, currentLine - contextLines);
        var endLine = Math.Min(lines.Length - 1, currentLine + contextLines);

        // Only analyze relevant portion
        return string.Join("\n", lines[startLine..endLine]);
    }

    private static int GetLineNumber(string text, int position)
    {
        return text.Substring(0, position).Count(c => c == '\n');
    }
}
```

## Error Handling

Implement robust error handling:

```csharp
public class SafeIntelliSenseProvider
{
    private readonly IntelliSenseProvider _provider;
    
    public List<CompletionSuggestion> GetCompletionsSafe(
        string query, int position, string database)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(query))
                return new List<CompletionSuggestion>();

            if (position < 0 || position > query.Length)
                return new List<CompletionSuggestion>();

            // Call with timeout
            return GetCompletionsWithTimeout(query, position, database, timeoutMs: 1000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IntelliSense error: {ex.Message}");
            return new List<CompletionSuggestion>(); // Return empty on error
        }
    }

    private List<CompletionSuggestion> GetCompletionsWithTimeout(
        string query, int position, string database, int timeoutMs)
    {
        var task = Task.Run(() => 
            _provider.GetCompletions(query, position, database));

        if (task.Wait(timeoutMs))
            return task.Result;
        else
            return new List<CompletionSuggestion>(); // Timeout
    }
}
```

## Testing Integration

Create a test harness to verify integration:

```csharp
[TestClass]
public class EditorIntegrationTests
{
    [TestMethod]
    public void TestCompletionPopupShows()
    {
        var editor = CreateMockTextView();
        var extension = new SqlEditorExtension();

        extension.TextViewCreated(editor);

        // Simulate typing
        SimulateTyping(editor, "SELECT * FROM Us");
        
        // Verify popup appears
        Assert.IsTrue(_completionPopup.IsVisible);
        Assert.IsTrue(_completionPopup.Suggestions.Count > 0);
    }
}
```

## Performance Profiling

Monitor performance:

```csharp
public class PerformanceMonitor
{
    public void MeasureCompletion(string query, int position, string database)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var suggestions = _provider.GetCompletions(query, position, database);
        stopwatch.Stop();

        System.Diagnostics.Debug.WriteLine(
            $"Completions generated in {stopwatch.ElapsedMilliseconds}ms. " +
            $"Count: {suggestions.Count}");
    }
}
```

## Summary

The integration flow:
1. SSMS loads your extension DLL
2. TEATimePackage initializes and hooks events
3. When user types in editor, text changed event fires
4. IntelliSenseProvider analyzes the query
5. CompletionPopup displays suggestions
6. User selects or dismisses

This creates a seamless experience similar to TEATime!
