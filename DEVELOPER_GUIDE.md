# TEATime SSMS Extension - Developer Guide

## Overview

This is an MVP (Minimum Viable Product) SSMS extension that provides IntelliSense-like code completion and code snippets. The extension offers intelligent suggestions for SQL keywords, table names, column names, and code templates.

## Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────┐
│         SSMS Editor Integration Layer                │
│   (Event hooks, text buffer monitoring)              │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│         IntelliSenseProvider (Core Engine)           │
│  - Context analysis                                  │
│  - Suggestion generation                             │
│  - Relevance scoring                                 │
└──────────────┬────────────────┬─────────────────────┘
               │                │
       ┌───────▼──────┐    ┌────▼──────────────┐
       │ Metadata     │    │ Keyword/Snippet   │
       │ Cache        │    │ Providers          │
       │ (Tables,     │    │ (Templates)        │
       │  Columns,    │    │                    │
       │  Schemas)    │    │                    │
       └──────────────┘    └────────────────────┘
```

### Core Classes

1. **TEATimePackage.cs**
   - Entry point for the SSMS extension
   - Implements IVsPackage interface
   - Responsible for initialization and cleanup
   - Registers event handlers with SSMS

2. **IntelliSenseProvider.cs**
   - Main intelligence engine
   - Analyzes SQL code at cursor position
   - Determines context (table, column, keyword, snippet)
   - Generates and ranks completion suggestions
   - Key method: `GetCompletions(query, cursorPosition, database)`

3. **DatabaseMetadataCache.cs**
   - Caches database object metadata (tables, columns, schemas)
   - Avoids repeated database queries for performance
   - Implements cache expiration (30-minute TTL)
   - In MVP: Uses sample data; production version connects to SQL Server

4. **SqlKeywordSuggester.cs**
   - Provides T-SQL keywords (140+ keywords)
   - Includes aggregate functions (SUM, AVG, COUNT, etc.)
   - String functions (CONCAT, SUBSTRING, etc.)
   - Date functions (GETDATE, DATEADD, etc.)
   - Math functions and data types

5. **SnippetProvider.cs**
   - Manages code templates (25+ snippets)
   - Triggers: "sel", "upd", "ins", "del", "cte", "func", etc.
   - Supports placeholder substitution ($1, $2, etc.)

6. **CompletionModels.cs**
   - Data structures for suggestions, metadata, snippets
   - Enums for completion types and context types

## Context Detection

The IntelliSenseProvider analyzes the SQL code to determine what type of completion to offer:

### ContextType Enum

- **Keyword**: T-SQL keywords and functions
  - Triggered when: At start of statement or after whitespace
  - Example: "SEL" → suggests "SELECT"

- **TableName**: Table/view names
  - Triggered when: After FROM, JOIN, UPDATE, DELETE, INTO
  - Example: "FROM U" → suggests "Users", "UserRoles"

- **ColumnName**: Column names from current database
  - Triggered when: After SELECT, WHERE, ON, GROUP BY, ORDER BY
  - Example: "SELECT U" → suggests "UserId", "Username"

- **SchemaPrefix**: Schema names (dbo, etc.)
  - Triggered when: Typing "schema." pattern
  - Example: "dbo." → suggests tables in dbo schema

- **Snippet**: Code templates
  - Triggered when: Typing snippet trigger words
  - Example: "sel" → expands to full SELECT template

## Getting Started

### Prerequisites

- Visual Studio 2019 or later (for SSMS compatibility)
- SQL Server Management Studio 18.0 or later
- .NET Framework 4.8.1 or .NET 5.0+
- C# knowledge

### Setup Steps

1. **Clone/Download the extension code**
   ```bash
   git clone <repository-url>
   cd TEATime
   ```

2. **Open in Visual Studio**
   - Create a new Class Library project
   - Copy the .cs files into the project
   - Update the .csproj to reference SSMS assemblies (see below)

3. **Reference SSMS Assemblies**
   
   The project needs references to SSMS components. Update your .csproj:
   
   ```xml
   <ItemGroup>
       <Reference Include="Microsoft.SqlServer.Management.UI.Grid">
           <HintPath>C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\Common7\IDE\Microsoft.SqlServer.Management.UI.Grid.dll</HintPath>
       </Reference>
       <!-- Add other SSMS references -->
   </ItemGroup>
   ```
   
   Default SSMS paths:
   - SSMS 20.x: `C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\`
   - SSMS 19.x: `C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\`
   - SSMS 18.x: `C:\Program Files (x86)\Microsoft SQL Server\140\Tools\Binn\ManagementStudio\`

4. **Create a VSIX Installer Project (Optional but Recommended)**
   
   For distribution, create a VSIX project that packages your extension.

5. **Build the Project**
   ```bash
   dotnet build
   ```

## Extension Registration (Registry)

For SSMS to load your extension, add registry entries:

```reg
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQL Server Management Studio\[Version]\Packages\{A1B2C3D4-E5F6-47A8-9B0C-1D2E3F4A5B6C}]
@="TEATime"
"InprocServer32"="[Path to your DLL]"
"Class"="TEATime.TEATimePackage"
```

**Important**: Change the GUID in TEATimePackage.cs to match your registry entry.

## Development Workflow

### Adding a New Keyword

Edit `SqlKeywordSuggester.cs`:

```csharp
keywords.Add(("MYKEYWORD", "Description of what it does", CompletionType.Keyword));
```

### Adding a New Snippet

Edit `SnippetProvider.cs`:

```csharp
new Snippet
{
    Trigger = "mysnip",
    DisplayName = "My Snippet",
    Description = "What this snippet does",
    Template = @"Template text here with $1, $2 placeholders"
}
```

### Connecting to Real Database

In `DatabaseMetadataCache.cs`, replace `LoadSampleMetadata()` with actual SQL queries:

```csharp
private void LoadDatabaseMetadata(string databaseName)
{
    try
    {
        using (var connection = new SqlConnection($"Server=.;Database={databaseName};Trusted_Connection=true;"))
        {
            connection.Open();
            
            // Load tables
            const string tableQuery = @"
                SELECT TABLE_SCHEMA, TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'
            ";
            
            using (var command = new SqlCommand(tableQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Parse results and populate _tableCache
                }
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error: {ex.Message}");
    }
}
```

## MVP Features

✅ **Implemented:**
- T-SQL keyword completion (140+ keywords)
- Function suggestions (aggregate, string, date, math)
- Data type completions
- Table name autocompletion
- Column name suggestions
- Schema prefix suggestions
- Code snippets (25+ templates)
- Context-aware suggestions
- Relevance scoring and sorting
- Metadata caching with TTL

**Not in MVP (Future Enhancements):**
- ❌ Real-time database connection and metadata loading
- ❌ Visual UI popup for suggestions
- ❌ Keyboard navigation of suggestions
- ❌ Parameter hinting for functions
- ❌ Query execution within the extension
- ❌ Query formatting and beautification
- ❌ Query analysis/optimization suggestions
- ❌ Custom snippet management UI
- ❌ Integration with SSMS event system

## Testing

Create a unit test project:

```csharp
[TestClass]
public class IntelliSenseProviderTests
{
    [TestMethod]
    public void TestTableNameCompletion()
    {
        var provider = new IntelliSenseProvider();
        var results = provider.GetCompletions("SELECT * FROM Us", 18, "master");
        
        Assert.IsTrue(results.Any(r => r.DisplayText == "Users"));
        Assert.IsTrue(results[0].Type == CompletionType.Table);
    }
}
```

## Performance Considerations

1. **Caching**: Database metadata is cached for 30 minutes
2. **Limiting Results**: Only top 25 suggestions returned
3. **Lazy Loading**: Metadata only loaded when needed
4. **Async Operations**: Consider using async database queries in production

## Debugging

Enable debug output in Visual Studio:

```csharp
System.Diagnostics.Debug.WriteLine("Debug message");
```

View output in Debug → Windows → Output panel.

## Common Issues & Solutions

### Issue: SSMS doesn't load the extension
**Solution**: 
- Verify GUID matches in code and registry
- Check that DLL path in registry is correct
- Ensure .NET Framework version matches

### Issue: No suggestions appearing
**Solution**:
- Verify context detection logic
- Check that metadata cache is populated
- Add debug output to trace execution

### Issue: Slow suggestions
**Solution**:
- Increase cache TTL
- Pre-load common databases
- Implement async loading

## Next Steps for Production

1. **Connect to Real Database**
   - Query INFORMATION_SCHEMA views
   - Load actual table/column metadata
   - Handle permission errors gracefully

2. **Implement UI Integration**
   - Hook into SSMS editor text buffer events
   - Show completion popup at cursor
   - Handle selection and insertion

3. **Add More Features**
   - Query formatting (SQL Formatter)
   - Query execution and result display
   - Basic query analysis/optimization hints

4. **Testing & QA**
   - Unit tests for all components
   - Integration tests with SSMS
   - Performance profiling

5. **Packaging & Distribution**
   - Create VSIX installer
   - Publish to Visual Studio Marketplace
   - Document installation process

## References

- [SSMS Extensibility](https://docs.microsoft.com/en-us/sql/ssms/extensions)
- [Visual Studio SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/visual-studio-sdk)
- [T-SQL Reference](https://docs.microsoft.com/en-us/sql/t-sql/language-reference)
- [SQL Server Object Explorer](https://docs.microsoft.com/en-us/sql/ssms/object/object-explorer)

## License

[Specify your license: MIT, Apache 2.0, etc.]

## Support & Contribution

For issues, feature requests, or contributions, please see [repository guidelines].
