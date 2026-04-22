namespace TEATime
{
    /// <summary>
    /// Represents a single completion suggestion
    /// </summary>
    public class CompletionSuggestion
    {
        /// <summary>
        /// Text shown in the IntelliSense dropdown
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Text that gets inserted when selected
        /// </summary>
        public string InsertionText { get; set; }

        /// <summary>
        /// Type of completion (affects icon and grouping)
        /// </summary>
        public CompletionType Type { get; set; }

        /// <summary>
        /// Descriptive text shown in tooltip
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Relevance score (0-100) for sorting
        /// Higher scores appear first
        /// </summary>
        public int Relevance { get; set; }

        /// <summary>
        /// Optional additional information
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Icon name or path for the suggestion
        /// </summary>
        public string Icon { get; set; }
    }

    /// <summary>
    /// Types of completions (affects display and categorization)
    /// </summary>
    public enum CompletionType
    {
        Keyword,
        Table,
        View,
        StoredProcedure,
        Function,
        Column,
        Schema,
        Database,
        Snippet,
        DataType,
        Parameter
    }

    /// <summary>
    /// Represents a single snippet (code template)
    /// </summary>
    public class Snippet
    {
        /// <summary>
        /// Trigger text (what the user types to invoke the snippet)
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Display name in the completion menu
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The actual code to insert
        /// $0 = cursor position, $1, $2 = placeholders
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Description of what the snippet does
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Database metadata for a table
    /// </summary>
    public class TableMetadata
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string Type { get; set; } // TABLE, VIEW, etc.
        public List<ColumnMetadata> Columns { get; set; } = new List<ColumnMetadata>();
    }

    /// <summary>
    /// Database metadata for a column
    /// </summary>
    public class ColumnMetadata
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
    }
}
