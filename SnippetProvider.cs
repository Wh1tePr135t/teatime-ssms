using System;
using System.Collections.Generic;
using System.Linq;

namespace TEATime
{
    /// <summary>
    /// SnippetProvider
    /// 
    /// Provides code snippets (templates) for common SQL patterns.
    /// Similar to SQL Prompt's snippet functionality.
    /// Users can type short triggers (e.g., "sel") to expand to full templates.
    /// </summary>
    public class SnippetProvider
    {
        private readonly List<Snippet> _snippets;

        public SnippetProvider()
        {
            _snippets = InitializeSnippets();
        }

        /// <summary>
        /// Get snippets matching the given trigger
        /// </summary>
        public List<CompletionSuggestion> GetSnippets(string trigger)
        {
            var suggestions = new List<CompletionSuggestion>();

            if (string.IsNullOrWhiteSpace(trigger))
                return suggestions;

            var matches = _snippets
                .Where(s => s.Trigger.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var snippet in matches)
            {
                suggestions.Add(new CompletionSuggestion
                {
                    DisplayText = snippet.DisplayName,
                    InsertionText = snippet.Template,
                    Type = CompletionType.Snippet,
                    Description = snippet.Description,
                    Relevance = 100,
                    Icon = "snippet"
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Initialize the snippet library
        /// </summary>
        private List<Snippet> InitializeSnippets()
        {
            return new List<Snippet>
            {
                // SELECT snippet
                new Snippet
                {
                    Trigger = "sel",
                    DisplayName = "SELECT Statement",
                    Description = "Basic SELECT query",
                    Template = @"SELECT $1
FROM $2
WHERE $3"
                },

                // SELECT with JOIN
                new Snippet
                {
                    Trigger = "join",
                    DisplayName = "SELECT with JOIN",
                    Description = "SELECT with INNER JOIN",
                    Template = @"SELECT $1
FROM $2 t1
INNER JOIN $3 t2 ON t1.$4 = t2.$4
WHERE $5"
                },

                // INSERT snippet
                new Snippet
                {
                    Trigger = "ins",
                    DisplayName = "INSERT Statement",
                    Description = "Basic INSERT query",
                    Template = @"INSERT INTO $1 ($2)
VALUES ($3)"
                },

                // UPDATE snippet
                new Snippet
                {
                    Trigger = "upd",
                    DisplayName = "UPDATE Statement",
                    Description = "Basic UPDATE query",
                    Template = @"UPDATE $1
SET $2 = $3
WHERE $4"
                },

                // DELETE snippet
                new Snippet
                {
                    Trigger = "del",
                    DisplayName = "DELETE Statement",
                    Description = "Basic DELETE query",
                    Template = @"DELETE FROM $1
WHERE $2"
                },

                // CREATE TABLE
                new Snippet
                {
                    Trigger = "ct",
                    DisplayName = "CREATE TABLE",
                    Description = "Create new table",
                    Template = @"CREATE TABLE $1 (
    $2Id INT PRIMARY KEY IDENTITY(1,1),
    $3 NVARCHAR(255) NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
)"
                },

                // Common Table Expression (CTE)
                new Snippet
                {
                    Trigger = "cte",
                    DisplayName = "Common Table Expression",
                    Description = "WITH clause for CTE",
                    Template = @"WITH $1 AS (
    SELECT $2
    FROM $3
    WHERE $4
)
SELECT $5
FROM $1"
                },

                // CASE Statement
                new Snippet
                {
                    Trigger = "case",
                    DisplayName = "CASE Statement",
                    Description = "Conditional expression",
                    Template = @"CASE
    WHEN $1 THEN $2
    WHEN $3 THEN $4
    ELSE $5
END"
                },

                // User-Defined Function
                new Snippet
                {
                    Trigger = "func",
                    DisplayName = "Scalar Function",
                    Description = "Create scalar function",
                    Template = @"CREATE FUNCTION $1 (@$2 $3)
RETURNS $4
AS
BEGIN
    DECLARE @result $4
    -- Function body
    RETURN @result
END"
                },

                // Stored Procedure
                new Snippet
                {
                    Trigger = "proc",
                    DisplayName = "Stored Procedure",
                    Description = "Create stored procedure",
                    Template = @"CREATE PROCEDURE $1
    @$2 $3 = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT *
    FROM $4
    WHERE $5
END"
                },

                // Trigger
                new Snippet
                {
                    Trigger = "trig",
                    DisplayName = "Trigger",
                    Description = "Create DML trigger",
                    Template = @"CREATE TRIGGER $1
ON $2
AFTER INSERT
AS
BEGIN
    -- Trigger body
END"
                },

                // Index
                new Snippet
                {
                    Trigger = "idx",
                    DisplayName = "CREATE INDEX",
                    Description = "Create index on table",
                    Template = @"CREATE INDEX $1
ON $2 ($3)
WHERE $4"
                },

                // ISNULL
                new Snippet
                {
                    Trigger = "isnull",
                    DisplayName = "ISNULL Function",
                    Description = "Handle NULL values",
                    Template = "ISNULL($1, $2)"
                },

                // COALESCE
                new Snippet
                {
                    Trigger = "coal",
                    DisplayName = "COALESCE Function",
                    Description = "Return first non-NULL value",
                    Template = "COALESCE($1, $2, $3)"
                },

                // EXISTS
                new Snippet
                {
                    Trigger = "exists",
                    DisplayName = "EXISTS Subquery",
                    Description = "Check subquery results",
                    Template = @"WHERE EXISTS (
    SELECT 1
    FROM $1
    WHERE $2
)"
                },

                // IN subquery
                new Snippet
                {
                    Trigger = "in",
                    DisplayName = "IN Subquery",
                    Description = "Check value in subquery",
                    Template = @"WHERE $1 IN (
    SELECT $2
    FROM $3
    WHERE $4
)"
                },

                // GROUP BY with aggregates
                new Snippet
                {
                    Trigger = "grp",
                    DisplayName = "GROUP BY Aggregate",
                    Description = "GROUP BY with aggregate functions",
                    Template = @"SELECT $1, COUNT(*) AS Count, SUM($2) AS Total
FROM $3
GROUP BY $1
HAVING COUNT(*) > $4"
                },

                // BETWEEN
                new Snippet
                {
                    Trigger = "between",
                    DisplayName = "BETWEEN Clause",
                    Description = "Range condition",
                    Template = "BETWEEN $1 AND $2"
                },

                // LIKE pattern
                new Snippet
                {
                    Trigger = "like",
                    DisplayName = "LIKE Pattern",
                    Description = "Pattern matching",
                    Template = "LIKE '$1%'"
                },

                // Transaction
                new Snippet
                {
                    Trigger = "tx",
                    DisplayName = "Transaction Block",
                    Description = "BEGIN/COMMIT/ROLLBACK",
                    Template = @"BEGIN TRANSACTION
    BEGIN TRY
        $1
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH"
                },

                // TRY/CATCH
                new Snippet
                {
                    Trigger = "try",
                    DisplayName = "TRY/CATCH Block",
                    Description = "Error handling",
                    Template = @"BEGIN TRY
    $1
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber,
           ERROR_MESSAGE() AS ErrorMessage
END CATCH"
                },

                // CAST
                new Snippet
                {
                    Trigger = "cast",
                    DisplayName = "CAST Expression",
                    Description = "Type conversion",
                    Template = "CAST($1 AS $2)"
                },

                // CONVERT
                new Snippet
                {
                    Trigger = "convert",
                    DisplayName = "CONVERT Function",
                    Description = "Type conversion with format",
                    Template = "CONVERT($1, $2, $3)"
                }
            };
        }

        /// <summary>
        /// Get all available snippets (for help/reference)
        /// </summary>
        public List<Snippet> GetAllSnippets()
        {
            return _snippets.OrderBy(s => s.Trigger).ToList();
        }
    }
}
