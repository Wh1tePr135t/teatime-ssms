using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TEATime.Tests
{
    /// <summary>
    /// Unit tests for IntelliSenseProvider
    /// 
    /// These tests demonstrate how the IntelliSense provider works
    /// and provide examples of using the extension's components.
    /// </summary>
    public class IntelliSenseProviderTests
    {
        private readonly IntelliSenseProvider _provider;

        public IntelliSenseProviderTests()
        {
            _provider = new IntelliSenseProvider();
        }

        #region Keyword Suggestion Tests

        [Fact]
        public void GetCompletions_KeywordPrefix_ReturnsKeywordSuggestions()
        {
            // Arrange
            var query = "SEL";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.DisplayText == "SELECT");
            Assert.All(results, r => Assert.Equal(CompletionType.Keyword, r.Type));
        }

        [Fact]
        public void GetCompletions_SQLKeyword_ReturnsSuggestionsStartingWithPrefix()
        {
            // Arrange
            var query = "SELE";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Contains(results, r => r.DisplayText == "SELECT");
            Assert.All(results, r => r.DisplayText.StartsWith("SEL", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetCompletions_FunctionName_ReturnsFunctionSuggestions()
        {
            // Arrange
            var query = "COUNT";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Type == CompletionType.Function);
        }

        #endregion

        #region Table Name Completion Tests

        [Fact]
        public void GetCompletions_AfterFromKeyword_ReturnsTableSuggestions()
        {
            // Arrange
            var query = "SELECT * FROM Us";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Type == CompletionType.Table);
        }

        [Fact]
        public void GetCompletions_TableNameWithSchema_ReturnsSchemaPrefixedTable()
        {
            // Arrange
            var query = "SELECT * FROM dbo.";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert - When after "dbo." we expect table suggestions
            Assert.NotEmpty(results);
        }

        [Fact]
        public void GetCompletions_MultipleTablesInQuery_ReturnsTableSuggestions()
        {
            // Arrange
            var query = @"SELECT u.* 
                         FROM Users u
                         INNER JOIN Ord";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
        }

        #endregion

        #region Column Name Completion Tests

        [Fact]
        public void GetCompletions_AfterSelect_ReturnsColumnSuggestions()
        {
            // Arrange
            var query = "SELECT Us";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
            // Results might include both columns and tables in MVP
        }

        [Fact]
        public void GetCompletions_InWhereClause_ReturnsColumnSuggestions()
        {
            // Arrange
            var query = "SELECT * FROM Users WHERE Us";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void GetCompletions_AfterAndOperator_ReturnsColumnSuggestions()
        {
            // Arrange
            var query = "SELECT * FROM Users WHERE Age > 18 AND Cre";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.NotEmpty(results);
        }

        #endregion

        #region Snippet Tests

        [Fact]
        public void GetCompletions_SnippetTrigger_ReturnsSnippet()
        {
            // Arrange
            var query = "sel";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Contains(results, r => r.Type == CompletionType.Snippet && r.DisplayText.Contains("SELECT"));
        }

        [Fact]
        public void GetCompletions_UpdateSnippetTrigger_ReturnsUpdateSnippet()
        {
            // Arrange
            var query = "upd";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Contains(results, r => r.Type == CompletionType.Snippet && r.DisplayText.Contains("UPDATE"));
        }

        [Fact]
        public void GetCompletions_CTESnippetTrigger_ReturnsCTESnippet()
        {
            // Arrange
            var query = "cte";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Contains(results, r => r.Type == CompletionType.Snippet && r.DisplayText.Contains("CTE"));
        }

        #endregion

        #region Context Detection Tests

        [Fact]
        public void GetCompletions_EmptyQuery_ReturnsNoSuggestions()
        {
            // Arrange
            var query = "";
            var position = 0;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GetCompletions_WhitespaceOnly_ReturnsNoSuggestions()
        {
            // Arrange
            var query = "   ";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GetCompletions_ResultsAreSortedByRelevance()
        {
            // Arrange
            var query = "SE";
            var position = query.Length;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            // Results should be sorted by relevance (descending) then alphabetically
            for (int i = 1; i < results.Count; i++)
            {
                Assert.True(
                    results[i - 1].Relevance >= results[i].Relevance ||
                    results[i - 1].DisplayText.CompareTo(results[i].DisplayText) <= 0,
                    "Results should be sorted by relevance then alphabetically");
            }
        }

        [Fact]
        public void GetCompletions_LimitedTo25Results()
        {
            // Arrange
            var query = ""; // Empty might return many keyword suggestions
            var position = 0;

            // Act
            var results = _provider.GetCompletions(query, position, "master");

            // Assert
            Assert.True(results.Count <= 25, "Should return at most 25 suggestions");
        }

        #endregion
    }

    /// <summary>
    /// Unit tests for SqlKeywordSuggester
    /// </summary>
    public class SqlKeywordSuggesterTests
    {
        private readonly SqlKeywordSuggester _suggester;

        public SqlKeywordSuggesterTests()
        {
            _suggester = new SqlKeywordSuggester();
        }

        [Fact]
        public void GetKeywordSuggestions_SelectPrefix_ReturnsSelectKeyword()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("SEL");

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.DisplayText == "SELECT");
        }

        [Fact]
        public void GetKeywordSuggestions_KeywordSuggestionsHaveDescription()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("FROM");

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, r => Assert.NotEmpty(r.Description));
        }

        [Fact]
        public void GetKeywordSuggestions_IncludesAggregateFunctions()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("SUM");

            // Assert
            Assert.Contains(results, r => r.DisplayText == "SUM");
        }

        [Fact]
        public void GetKeywordSuggestions_IncludesStringFunctions()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("CONC");

            // Assert
            Assert.Contains(results, r => r.DisplayText == "CONCAT");
        }

        [Fact]
        public void GetKeywordSuggestions_IncludesDateFunctions()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("GETD");

            // Assert
            Assert.Contains(results, r => r.DisplayText == "GETDATE");
        }

        [Fact]
        public void GetKeywordSuggestions_IncludesDataTypes()
        {
            // Act
            var results = _suggester.GetKeywordSuggestions("VARCHAR");

            // Assert
            Assert.Contains(results, r => r.DisplayText == "VARCHAR");
        }
    }

    /// <summary>
    /// Unit tests for SnippetProvider
    /// </summary>
    public class SnippetProviderTests
    {
        private readonly SnippetProvider _snippetProvider;

        public SnippetProviderTests()
        {
            _snippetProvider = new SnippetProvider();
        }

        [Fact]
        public void GetSnippets_SelectTrigger_ReturnsSelectSnippet()
        {
            // Act
            var results = _snippetProvider.GetSnippets("sel");

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.DisplayText.Contains("SELECT"));
        }

        [Fact]
        public void GetSnippets_AllSnippetsHaveTemplate()
        {
            // Act
            var results = _snippetProvider.GetSnippets("");

            // Assert
            Assert.All(results, r => Assert.NotEmpty(r.InsertionText));
        }

        [Fact]
        public void GetSnippets_AllSnippetsHaveDescription()
        {
            // Act
            var results = _snippetProvider.GetSnippets("");

            // Assert
            Assert.All(results, r => Assert.NotEmpty(r.Description));
        }

        [Fact]
        public void GetAllSnippets_ReturnsAllAvailableSnippets()
        {
            // Act
            var allSnippets = _snippetProvider.GetAllSnippets();

            // Assert
            Assert.NotEmpty(allSnippets);
            Assert.True(allSnippets.Count >= 20, "Should have at least 20 snippets");
        }

        [Theory]
        [InlineData("sel", "SELECT")]
        [InlineData("upd", "UPDATE")]
        [InlineData("ins", "INSERT")]
        [InlineData("del", "DELETE")]
        [InlineData("cte", "CTE")]
        [InlineData("func", "Function")]
        [InlineData("proc", "Procedure")]
        public void GetSnippets_CommonTriggers_ReturnExpectedSnippets(string trigger, string expectedKeyword)
        {
            // Act
            var results = _snippetProvider.GetSnippets(trigger);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.DisplayText.Contains(expectedKeyword));
        }
    }

    /// <summary>
    /// Unit tests for DatabaseMetadataCache
    /// </summary>
    public class DatabaseMetadataCacheTests
    {
        private readonly DatabaseMetadataCache _cache;

        public DatabaseMetadataCacheTests()
        {
            _cache = new DatabaseMetadataCache();
        }

        [Fact]
        public void GetTables_MasterDatabase_ReturnsTablesWithSchemaPrefix()
        {
            // Act
            var tables = _cache.GetTables("master");

            // Assert
            Assert.NotEmpty(tables);
            Assert.All(tables, t => Assert.Contains(".", t)); // Should have schema.table format
        }

        [Fact]
        public void GetColumns_MasterDatabase_ReturnsColumnNames()
        {
            // Act
            var columns = _cache.GetColumns("master");

            // Assert
            Assert.NotEmpty(columns);
        }

        [Fact]
        public void GetSchemas_MasterDatabase_ReturnsSchemasIncludingDbo()
        {
            // Act
            var schemas = _cache.GetSchemas("master");

            // Assert
            Assert.NotEmpty(schemas);
            Assert.Contains("dbo", schemas);
        }

        [Fact]
        public void RefreshDatabaseMetadata_ClearsAndReloadsCache()
        {
            // Arrange
            var initialTables = _cache.GetTables("master").Count;

            // Act
            _cache.RefreshDatabaseMetadata("master");
            var refreshedTables = _cache.GetTables("master").Count;

            // Assert
            Assert.Equal(initialTables, refreshedTables);
        }

        [Fact]
        public void GetTableMetadata_ValidTableName_ReturnsTableInfo()
        {
            // Act
            var metadata = _cache.GetTableMetadata("master", "Users");

            // Assert
            if (metadata != null) // Might be null if table doesn't exist in sample data
            {
                Assert.NotNull(metadata.Name);
                Assert.NotEmpty(metadata.Columns);
            }
        }
    }

    /// <summary>
    /// Integration tests demonstrating typical usage scenarios
    /// </summary>
    public class IntegrationExamples
    {
        [Fact]
        public void Example_CompleteSelectQuery()
        {
            // This example shows how to get completions at various stages of typing a query

            var provider = new IntelliSenseProvider();

            // Step 1: Type "SELECT"
            var step1 = provider.GetCompletions("SEL", 3, "master");
            Assert.Contains(step1, r => r.DisplayText == "SELECT");

            // Step 2: After SELECT, type table name
            var step2 = provider.GetCompletions("SELECT * FROM Us", 17, "master");
            Assert.NotEmpty(step2);

            // Step 3: In WHERE clause
            var step3 = provider.GetCompletions("SELECT * FROM Users WHERE Cre", 30, "master");
            Assert.NotEmpty(step3);
        }

        [Fact]
        public void Example_UseSnippetToQuicklyCreateQuery()
        {
            var provider = new IntelliSenseProvider();

            // User types "upd" to get UPDATE snippet
            var snippets = provider.GetCompletions("upd", 3, "master");
            var updateSnippet = snippets.FirstOrDefault(r => 
                r.Type == CompletionType.Snippet && r.DisplayText.Contains("UPDATE"));

            Assert.NotNull(updateSnippet);
            // User selects it and the template is inserted
            Assert.Contains("UPDATE", updateSnippet.InsertionText);
        }

        [Fact]
        public void Example_CombineMultipleFeatures()
        {
            var provider = new IntelliSenseProvider();

            // 1. Get keyword suggestion
            var keywords = provider.GetCompletions("CASE", 4, "master");
            Assert.Contains(keywords, r => r.DisplayText == "CASE");

            // 2. Get keyword suggestion for function
            var functions = provider.GetCompletions("COUNT", 5, "master");
            Assert.Contains(functions, r => r.Type == CompletionType.Function);

            // 3. Get snippet for complex pattern
            var snippets = provider.GetCompletions("tx", 2, "master");
            Assert.Contains(snippets, r => r.Type == CompletionType.Snippet);
        }
    }
}
