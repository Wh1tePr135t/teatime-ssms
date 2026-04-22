using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TEATime
{
    /// <summary>
    /// IntelliSenseProvider
    /// 
    /// Core component that generates intelligent code completion suggestions.
    /// This is the heart of the SQL Prompt-like functionality.
    /// 
    /// MVP Features:
    /// - Database object autocompletion (tables, views, stored procedures)
    /// - T-SQL keyword suggestions
    /// - Column name autocompletion
    /// - Snippet expansion
    /// </summary>
    public class IntelliSenseProvider : IDisposable
    {
        private readonly DatabaseMetadataCache _metadataCache;
        private readonly SqlKeywordSuggester _keywordSuggester;
        private readonly SnippetProvider _snippetProvider;

        public IntelliSenseProvider()
        {
            _metadataCache = new DatabaseMetadataCache();
            _keywordSuggester = new SqlKeywordSuggester();
            _snippetProvider = new SnippetProvider();
        }

        /// <summary>
        /// Get completion suggestions for the current cursor position
        /// </summary>
        /// <param name="query">The SQL query being edited</param>
        /// <param name="cursorPosition">Current position in the query</param>
        /// <param name="databaseName">Current database context</param>
        /// <returns>List of completion suggestions</returns>
        public List<CompletionSuggestion> GetCompletions(
            string query,
            int cursorPosition,
            string databaseName)
        {
            var suggestions = new List<CompletionSuggestion>();

            if (string.IsNullOrWhiteSpace(query))
                return suggestions;

            // Get the word/context at cursor position
            var context = ExtractContextAtCursor(query, cursorPosition);

            if (context == null)
                return suggestions;

            // Generate suggestions based on context
            switch (context.Type)
            {
                case ContextType.TableName:
                    suggestions.AddRange(GetTableSuggestions(context, databaseName));
                    break;

                case ContextType.ColumnName:
                    suggestions.AddRange(GetColumnSuggestions(context, databaseName));
                    break;

                case ContextType.Keyword:
                    suggestions.AddRange(GetKeywordSuggestions(context));
                    break;

                case ContextType.SchemaPrefix:
                    suggestions.AddRange(GetSchemaSuggestions(context, databaseName));
                    break;

                case ContextType.Snippet:
                    suggestions.AddRange(GetSnippetSuggestions(context));
                    break;
            }

            // Sort by relevance and limit results
            return suggestions
                .OrderByDescending(s => s.Relevance)
                .ThenBy(s => s.DisplayText)
                .Take(25)
                .ToList();
        }

        /// <summary>
        /// Extract the context around the cursor position
        /// </summary>
        private EditorContext ExtractContextAtCursor(string query, int cursorPosition)
        {
            if (cursorPosition > query.Length)
                cursorPosition = query.Length;

            var context = new EditorContext();

            // Get the word at cursor (handle partial words)
            var wordMatch = GetWordAtCursor(query, cursorPosition);
            if (wordMatch == null)
                return null;

            context.CurrentWord = wordMatch.Value;
            context.StartPosition = wordMatch.Index;
            context.EndPosition = wordMatch.Index + wordMatch.Length;

            // Analyze the SQL context
            var precedingText = query.Substring(0, cursorPosition);
            var tokens = TokenizeSql(precedingText);

            // Determine what kind of suggestions are appropriate
            DetermineContextType(context, tokens, precedingText);

            return context;
        }

        /// <summary>
        /// Find the word at the given cursor position
        /// </summary>
        private Match GetWordAtCursor(string text, int position)
        {
            // Pattern for SQL identifiers (table names, columns, keywords)
            var pattern = @"[A-Za-z_][A-Za-z0-9_]*";
            var matches = Regex.Matches(text, pattern);

            // Find the word containing or just before the cursor
            foreach (Match match in matches)
            {
                if (match.Index + match.Length >= position - 1)
                    return match;
            }

            return null;
        }

        /// <summary>
        /// Simple SQL tokenizer
        /// </summary>
        private List<string> TokenizeSql(string sql)
        {
            var pattern = @"[A-Za-z_][A-Za-z0-9_]*|[(),]|'[^']*'|--[^\n]*";
            return Regex.Matches(sql, pattern)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        /// Determine the type of context (table, column, keyword, etc.)
        /// </summary>
        private void DetermineContextType(EditorContext context, List<string> tokens, string precedingText)
        {
            // Simple heuristic-based context detection
            var upperTokens = tokens.Select(t => t.ToUpper()).ToList();

            // Check for schema.object pattern (dbo.TableName)
            if (precedingText.EndsWith(".") && precedingText.Length > 1)
            {
                context.Type = ContextType.SchemaPrefix;
                return;
            }

            // Check for column context (after SELECT, WHERE, ON, etc.)
            var columnKeywords = new[] { "SELECT", "WHERE", "AND", "OR", "ON", "JOIN", "GROUP", "ORDER" };
            var lastColumnKeyword = upperTokens.FindLastIndex(t => columnKeywords.Contains(t));

            if (lastColumnKeyword >= 0 && 
                !upperTokens.Skip(lastColumnKeyword + 1).Any(t => new[] { "FROM", "WHERE", "GROUP", "ORDER" }.Contains(t)))
            {
                context.Type = ContextType.ColumnName;
                return;
            }

            // Check for table context (after FROM, JOIN, INTO, etc.)
            var tableKeywords = new[] { "FROM", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "CROSS", "INTO", "UPDATE" };
            if (upperTokens.Any(t => tableKeywords.Contains(t)))
            {
                context.Type = ContextType.TableName;
                return;
            }

            // Check for keyword context
            if (IsLikelyKeyword(context.CurrentWord))
            {
                context.Type = ContextType.Keyword;
                return;
            }

            // Check for snippet
            if (IsSnippetTrigger(context.CurrentWord))
            {
                context.Type = ContextType.Snippet;
                return;
            }

            // Default to keyword suggestions
            context.Type = ContextType.Keyword;
        }

        /// <summary>
        /// Get table name suggestions
        /// </summary>
        private List<CompletionSuggestion> GetTableSuggestions(EditorContext context, string databaseName)
        {
            var suggestions = new List<CompletionSuggestion>();
            var tables = _metadataCache.GetTables(databaseName);

            var filtered = tables
                .Where(t => t.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var table in filtered)
            {
                suggestions.Add(new CompletionSuggestion
                {
                    DisplayText = table,
                    InsertionText = table,
                    Type = CompletionType.Table,
                    Description = $"Table: {table}",
                    Relevance = CalculateRelevance(table, context.CurrentWord)
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Get column name suggestions
        /// </summary>
        private List<CompletionSuggestion> GetColumnSuggestions(EditorContext context, string databaseName)
        {
            var suggestions = new List<CompletionSuggestion>();
            var columns = _metadataCache.GetColumns(databaseName);

            var filtered = columns
                .Where(c => c.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var column in filtered)
            {
                suggestions.Add(new CompletionSuggestion
                {
                    DisplayText = column,
                    InsertionText = column,
                    Type = CompletionType.Column,
                    Description = $"Column: {column}",
                    Relevance = CalculateRelevance(column, context.CurrentWord)
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Get keyword suggestions
        /// </summary>
        private List<CompletionSuggestion> GetKeywordSuggestions(EditorContext context)
        {
            return _keywordSuggester.GetKeywordSuggestions(context.CurrentWord);
        }

        /// <summary>
        /// Get schema suggestions
        /// </summary>
        private List<CompletionSuggestion> GetSchemaSuggestions(EditorContext context, string databaseName)
        {
            var suggestions = new List<CompletionSuggestion>();
            var schemas = _metadataCache.GetSchemas(databaseName);

            var filtered = schemas
                .Where(s => s.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var schema in filtered)
            {
                suggestions.Add(new CompletionSuggestion
                {
                    DisplayText = schema,
                    InsertionText = schema + ".",
                    Type = CompletionType.Schema,
                    Description = $"Schema: {schema}",
                    Relevance = 100
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Get snippet suggestions
        /// </summary>
        private List<CompletionSuggestion> GetSnippetSuggestions(EditorContext context)
        {
            return _snippetProvider.GetSnippets(context.CurrentWord);
        }

        /// <summary>
        /// Calculate relevance score for a suggestion
        /// Based on prefix match length and frequency
        /// </summary>
        private int CalculateRelevance(string suggestion, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return 50;

            var prefixMatch = Math.Min(prefix.Length, suggestion.Length);
            var exactPrefix = suggestion.Substring(0, prefixMatch).Equals(prefix, StringComparison.OrdinalIgnoreCase);

            return exactPrefix ? (prefix.Length * 20) : 10;
        }

        /// <summary>
        /// Check if a word is likely a SQL keyword
        /// </summary>
        private bool IsLikelyKeyword(string word)
        {
            var keywords = new[] 
            { 
                "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE",
                "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
                "GROUP", "ORDER", "BY", "HAVING", "DISTINCT", "TOP"
            };

            return keywords.Any(k => k.StartsWith(word.ToUpper()));
        }

        /// <summary>
        /// Check if a word is a snippet trigger
        /// </summary>
        private bool IsSnippetTrigger(string word)
        {
            var triggers = new[] { "sel", "upd", "ins", "del", "cte", "func" };
            return triggers.Contains(word.ToLower());
        }

        public void Dispose()
        {
            _metadataCache?.Dispose();
        }
    }

    /// <summary>
    /// Represents the editor context at cursor position
    /// </summary>
    public class EditorContext
    {
        public string CurrentWord { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public ContextType Type { get; set; }
    }

    /// <summary>
    /// Types of context for completion suggestions
    /// </summary>
    public enum ContextType
    {
        Keyword,
        TableName,
        ColumnName,
        SchemaPrefix,
        Snippet
    }
}
