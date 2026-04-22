using System;
using System.Collections.Generic;
using System.Linq;

namespace TEATime
{
    /// <summary>
    /// SqlKeywordSuggester
    /// 
    /// Provides intelligent suggestions for T-SQL keywords and built-in functions.
    /// Includes common keywords, functions, and data types.
    /// </summary>
    public class SqlKeywordSuggester
    {
        private readonly List<(string keyword, string description, CompletionType type)> _keywords;

        public SqlKeywordSuggester()
        {
            _keywords = InitializeKeywords();
        }

        /// <summary>
        /// Get keyword suggestions that match the given prefix
        /// </summary>
        public List<CompletionSuggestion> GetKeywordSuggestions(string prefix)
        {
            var suggestions = new List<CompletionSuggestion>();

            if (string.IsNullOrWhiteSpace(prefix))
                prefix = "";

            var prefixUpper = prefix.ToUpper();

            var matches = _keywords
                .Where(k => k.keyword.StartsWith(prefixUpper))
                .OrderBy(k => k.keyword)
                .Take(20)
                .ToList();

            foreach (var (keyword, description, type) in matches)
            {
                suggestions.Add(new CompletionSuggestion
                {
                    DisplayText = keyword,
                    InsertionText = keyword + " ",
                    Type = type,
                    Description = description,
                    Relevance = CalculateKeywordRelevance(keyword, prefix)
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Initialize the keyword list
        /// Includes T-SQL keywords, functions, and data types
        /// </summary>
        private List<(string keyword, string description, CompletionType type)> InitializeKeywords()
        {
            var keywords = new List<(string, string, CompletionType)>();

            // T-SQL Keywords (DDL, DML)
            var sqlKeywords = new[]
            {
                // SELECT Clause
                ("SELECT", "Retrieve rows from tables", CompletionType.Keyword),
                ("DISTINCT", "Remove duplicate rows", CompletionType.Keyword),
                ("TOP", "Limit number of rows returned", CompletionType.Keyword),
                ("ALL", "Include all rows (default)", CompletionType.Keyword),

                // FROM Clause
                ("FROM", "Specify source table(s)", CompletionType.Keyword),
                ("WHERE", "Filter rows with a condition", CompletionType.Keyword),
                ("AND", "Combine multiple conditions", CompletionType.Keyword),
                ("OR", "Alternative condition", CompletionType.Keyword),
                ("NOT", "Negate a condition", CompletionType.Keyword),

                // JOIN Operations
                ("JOIN", "Combine rows from multiple tables", CompletionType.Keyword),
                ("INNER", "Return matching rows only", CompletionType.Keyword),
                ("LEFT", "Include unmatched left table rows", CompletionType.Keyword),
                ("RIGHT", "Include unmatched right table rows", CompletionType.Keyword),
                ("FULL", "Include unmatched rows from both tables", CompletionType.Keyword),
                ("OUTER", "Include unmatched rows", CompletionType.Keyword),
                ("CROSS", "Cartesian product of tables", CompletionType.Keyword),
                ("ON", "Specify join condition", CompletionType.Keyword),

                // GROUP BY / ORDER BY
                ("GROUP", "Group rows by columns", CompletionType.Keyword),
                ("ORDER", "Sort result set", CompletionType.Keyword),
                ("BY", "Specify grouping or sorting column", CompletionType.Keyword),
                ("HAVING", "Filter grouped results", CompletionType.Keyword),
                ("ASC", "Ascending sort order", CompletionType.Keyword),
                ("DESC", "Descending sort order", CompletionType.Keyword),

                // INSERT/UPDATE/DELETE
                ("INSERT", "Add new rows to table", CompletionType.Keyword),
                ("INTO", "Specify target table", CompletionType.Keyword),
                ("VALUES", "Specify values to insert", CompletionType.Keyword),
                ("UPDATE", "Modify existing rows", CompletionType.Keyword),
                ("SET", "Assign new values", CompletionType.Keyword),
                ("DELETE", "Remove rows from table", CompletionType.Keyword),

                // CREATE/ALTER/DROP
                ("CREATE", "Create new database object", CompletionType.Keyword),
                ("ALTER", "Modify existing database object", CompletionType.Keyword),
                ("DROP", "Delete database object", CompletionType.Keyword),
                ("TABLE", "Database table", CompletionType.Keyword),
                ("VIEW", "Virtual table", CompletionType.Keyword),
                ("INDEX", "Data structure for fast lookup", CompletionType.Keyword),
                ("PROCEDURE", "Stored procedure", CompletionType.Keyword),
                ("FUNCTION", "User-defined function", CompletionType.Keyword),
                ("DATABASE", "Database container", CompletionType.Keyword),

                // Other Keywords
                ("CASE", "Conditional expression", CompletionType.Keyword),
                ("WHEN", "Case condition", CompletionType.Keyword),
                ("THEN", "Case result", CompletionType.Keyword),
                ("ELSE", "Default case result", CompletionType.Keyword),
                ("END", "End block", CompletionType.Keyword),
                ("BETWEEN", "Range condition", CompletionType.Keyword),
                ("IN", "Check membership", CompletionType.Keyword),
                ("LIKE", "Pattern matching", CompletionType.Keyword),
                ("IS", "Check NULL value", CompletionType.Keyword),
                ("NULL", "No value", CompletionType.Keyword),
                ("AS", "Alias", CompletionType.Keyword),
                ("WITH", "Common Table Expression", CompletionType.Keyword),
                ("UNION", "Combine query results", CompletionType.Keyword),
                ("EXCEPT", "Difference between result sets", CompletionType.Keyword),
                ("INTERSECT", "Common rows between result sets", CompletionType.Keyword),
            };

            keywords.AddRange(sqlKeywords);

            // Aggregate Functions
            var aggregateFunctions = new[]
            {
                ("SUM", "Calculate total", CompletionType.Function),
                ("AVG", "Calculate average", CompletionType.Function),
                ("COUNT", "Count rows", CompletionType.Function),
                ("MIN", "Find minimum value", CompletionType.Function),
                ("MAX", "Find maximum value", CompletionType.Function),
                ("STDEV", "Standard deviation", CompletionType.Function),
                ("VAR", "Variance", CompletionType.Function),
            };

            keywords.AddRange(aggregateFunctions);

            // String Functions
            var stringFunctions = new[]
            {
                ("CONCAT", "Concatenate strings", CompletionType.Function),
                ("SUBSTRING", "Extract substring", CompletionType.Function),
                ("LEN", "String length", CompletionType.Function),
                ("UPPER", "Convert to uppercase", CompletionType.Function),
                ("LOWER", "Convert to lowercase", CompletionType.Function),
                ("TRIM", "Remove leading/trailing spaces", CompletionType.Function),
                ("LTRIM", "Remove leading spaces", CompletionType.Function),
                ("RTRIM", "Remove trailing spaces", CompletionType.Function),
                ("REPLACE", "Replace text", CompletionType.Function),
                ("CHARINDEX", "Find substring position", CompletionType.Function),
            };

            keywords.AddRange(stringFunctions);

            // Date Functions
            var dateFunctions = new[]
            {
                ("GETDATE", "Current date and time", CompletionType.Function),
                ("GETUTCDATE", "Current UTC date and time", CompletionType.Function),
                ("DATEADD", "Add interval to date", CompletionType.Function),
                ("DATEDIFF", "Difference between dates", CompletionType.Function),
                ("EOMONTH", "End of month", CompletionType.Function),
                ("DATEFROMPARTS", "Create date from parts", CompletionType.Function),
                ("FORMAT", "Format value as string", CompletionType.Function),
            };

            keywords.AddRange(dateFunctions);

            // Math Functions
            var mathFunctions = new[]
            {
                ("ABS", "Absolute value", CompletionType.Function),
                ("ROUND", "Round number", CompletionType.Function),
                ("CEILING", "Round up", CompletionType.Function),
                ("FLOOR", "Round down", CompletionType.Function),
                ("POWER", "Raise to power", CompletionType.Function),
                ("SQRT", "Square root", CompletionType.Function),
            };

            keywords.AddRange(mathFunctions);

            // Data Types
            var dataTypes = new[]
            {
                ("INT", "Integer (4 bytes)", CompletionType.DataType),
                ("BIGINT", "Large integer (8 bytes)", CompletionType.DataType),
                ("SMALLINT", "Small integer (2 bytes)", CompletionType.DataType),
                ("TINYINT", "Tiny integer (1 byte)", CompletionType.DataType),
                ("DECIMAL", "Exact decimal", CompletionType.DataType),
                ("NUMERIC", "Exact numeric", CompletionType.DataType),
                ("FLOAT", "Approximate floating point", CompletionType.DataType),
                ("REAL", "Single precision floating point", CompletionType.DataType),
                ("VARCHAR", "Variable length string", CompletionType.DataType),
                ("NVARCHAR", "Unicode variable length string", CompletionType.DataType),
                ("CHAR", "Fixed length string", CompletionType.DataType),
                ("NCHAR", "Fixed length Unicode string", CompletionType.DataType),
                ("DATE", "Date only", CompletionType.DataType),
                ("TIME", "Time only", CompletionType.DataType),
                ("DATETIME", "Date and time", CompletionType.DataType),
                ("DATETIME2", "Date and time (higher precision)", CompletionType.DataType),
                ("BIT", "Boolean (0 or 1)", CompletionType.DataType),
                ("MONEY", "Monetary value", CompletionType.DataType),
                ("SMALLMONEY", "Small monetary value", CompletionType.DataType),
                ("UNIQUEIDENTIFIER", "GUID", CompletionType.DataType),
                ("BINARY", "Fixed length binary", CompletionType.DataType),
                ("VARBINARY", "Variable length binary", CompletionType.DataType),
                ("TEXT", "Large text", CompletionType.DataType),
                ("NTEXT", "Large Unicode text", CompletionType.DataType),
                ("IMAGE", "Large binary", CompletionType.DataType),
            };

            keywords.AddRange(dataTypes);

            return keywords;
        }

        /// <summary>
        /// Calculate relevance score for a keyword
        /// </summary>
        private int CalculateKeywordRelevance(string keyword, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return 50;

            var prefixLen = prefix.Length;
            var keywordLen = keyword.Length;

            // Prefer shorter matches (more specific)
            var lengthScore = 100 - (keywordLen * 5);

            // Prefer exact prefix matches
            var prefixScore = prefix.Equals(keyword.Substring(0, prefixLen), StringComparison.OrdinalIgnoreCase) ? 50 : 0;

            return Math.Max(0, lengthScore + prefixScore);
        }
    }
}
