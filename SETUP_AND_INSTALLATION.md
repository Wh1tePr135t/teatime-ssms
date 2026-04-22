# Setup and Installation Guide

This guide walks you through setting up the TEATime SSMS extension for development or deployment.

## System Requirements

- **Operating System**: Windows 10 or later
- **Visual Studio**: 2019 or 2022 (Community Edition is fine)
- **SQL Server Management Studio**: 18.0 or later
- **.NET Framework**: 4.7.2 or later
- **.NET SDK**: Optional but recommended for CLI builds
- **Administrator Access**: Required for registry modifications

## Development Setup

### Step 1: Install Visual Studio (if needed)

If you don't have Visual Studio installed:

1. Download from https://visualstudio.microsoft.com/vs/
2. Run the installer
3. Select "Desktop development with C#"
4. Install additional components:
   - Visual Studio SDK (for extension development)
   - .NET Framework 4.7.2 or later

### Step 2: Clone the Repository

```bash
# Using Git
git clone https://github.com/yourusername/teatime-ssms.git
cd teatime-ssms

# Or download as ZIP and extract
```

### Step 3: Open in Visual Studio

1. Launch Visual Studio
2. File → Open → Project/Solution
3. Navigate to the cloned directory
4. Select `TEATime.csproj`
5. Click "Open"

### Step 4: Resolve SSMS References

The project references SSMS assemblies. You need to update the paths:

1. **Find your SSMS installation path**:
   - SSMS 20.x: `C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\`
   - SSMS 19.x: `C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\`
   - SSMS 18.x: Check `C:\Program Files (x86)\Microsoft SQL Server\`

2. **Edit TEATime.csproj**:
   
   Open the file in a text editor and update the paths:
   
   ```xml
   <ItemGroup>
       <Reference Include="Microsoft.SqlServer.Management.UI.Grid">
           <HintPath>YOUR_SSMS_PATH\Common7\IDE\Microsoft.SqlServer.Management.UI.Grid.dll</HintPath>
       </Reference>
       <Reference Include="Microsoft.SqlServer.Management.UI.RSHost">
           <HintPath>YOUR_SSMS_PATH\Common7\IDE\Microsoft.SqlServer.Management.UI.RSHost.dll</HintPath>
       </Reference>
       <!-- Update other paths similarly -->
   </ItemGroup>
   ```

3. **Find SSMS Assemblies**:
   
   If you're unsure about paths, search for the DLLs:
   
   ```bash
   # In PowerShell
   $ssmsPath = "C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\"
   Get-ChildItem -Path $ssmsPath -Filter "Microsoft.SqlServer.Management.*.dll" -Recurse | Select FullName
   ```

### Step 5: Restore NuGet Packages (if any)

```bash
# In Visual Studio
# Tools → NuGet Package Manager → Package Manager Console
# Then run:
Update-Package

# Or via command line
dotnet restore
```

### Step 6: Build the Project

1. **In Visual Studio**:
   - Build → Build Solution
   - Or press Ctrl+Shift+B

2. **Or via command line**:
   ```bash
   dotnet build
   ```

### Step 7: Verify Build Success

- Check Output window (View → Output)
- Look for "Build succeeded" message
- Check for any warnings or errors
- The compiled DLL should be in `bin\Debug\` folder

## Registry Setup (For SSMS Integration)

For SSMS to load your extension, you need to register it in the Windows Registry:

### Option A: Manual Registry Editing

1. **Generate a unique GUID** (if you changed TEATimePackage.cs):
   - Tools → Create GUID in Visual Studio
   - Or use an online GUID generator

2. **Open Registry Editor**:
   - Press Win+R
   - Type `regedit`
   - Click OK

3. **Navigate to SSMS Extensions**:
   - SSMS 18.x:
     ```
     HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQL Server Management Studio\14.0\Packages
     ```
   - SSMS 19.x:
     ```
     HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQL Server Management Studio\15.0\Packages
     ```
   - SSMS 20.x:
     ```
     HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQL Server Management Studio\19.0\Packages
     ```

4. **Create New Key**:
   - Right-click → New → Key
   - Name: Your GUID (e.g., `{7DDEDD0D-028B-422C-B4B8-0377801ACFE5}`)

5. **Set Registry Values**:
   - Right-click the new key → New → String Value
   - Name: `(Default)`
   - Value: `TEATime`
   
   - Right-click → New → String Value
   - Name: `InprocServer32`
   - Value: Full path to your DLL (e.g., `C:\projects\teatime-ssms\bin\Debug\TEATime.dll`)
   
   - Right-click → New → String Value
   - Name: `Class`
   - Value: `TEATime.TEATimePackage`

### Option B: Automatic Registry Script

Create a `.reg` file with your GUID and DLL path:

```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQL Server Management Studio\19.0\Packages\{7DDEDD0D-028B-422C-B4B8-0377801ACFE5}]
@="TEATime"
"InprocServer32"="C:\\projects\\teatime-ssms\\bin\\Debug\\TEATime.dll"
"Class"="TEATime.TEATimePackage"
```

Save as `.reg` and double-click to import.

## Testing the Extension

### Step 1: Launch SSMS with Extension

1. Open a new instance of Visual Studio (the development instance)
2. Build the project
3. Close any running SSMS instances
4. Launch SSMS fresh
5. The extension should load silently

### Step 2: Verify Loading

1. Open SSMS
2. Connect to a SQL Server instance
3. Open a new query window
4. View → Output (to see debug messages)
5. Look for "TEATime extension initialized successfully"

### Step 3: Test Functionality

1. Type SQL code in the query window
2. Trigger suggestions:
   - Type `SEL` and wait for suggestions
   - Type `SELECT * FROM Us` to test table name completion
   - Type `sel` to trigger snippet expansion

### Troubleshooting Build Issues

#### Issue: Assembly not found

**Error**: "Could not find assembly Microsoft.SqlServer.Management.UI.Grid"

**Solution**:
- Verify SSMS installation path
- Check DLL file actually exists
- Ensure you're using correct version for your SSMS version

#### Issue: Version mismatch

**Error**: "Version mismatch between application and DLL"

**Solution**:
- Rebuild project with correct .NET Framework version
- Check TEATime.csproj `<TargetFramework>`
- Ensure it matches your SSMS compatibility

#### Issue: Missing dependencies

**Error**: "Missing reference to Microsoft.SqlServer.Management.Common"

**Solution**:
- Install SQL Server Management Objects (SMO) NuGet package
- Or copy from SSMS installation directory

## Debugging

### Enable Debug Output

1. In Visual Studio, go to Debug → Windows → Output
2. Select "Debug" from the dropdown
3. Run your code and watch the output window

### Debug IntelliSense Provider

Add breakpoints and debug the `GetCompletions` method:

```csharp
public List<CompletionSuggestion> GetCompletions(
    string query,
    int cursorPosition,
    string databaseName)
{
    // Set breakpoint here
    var suggestions = new List<CompletionSuggestion>();
    
    if (string.IsNullOrWhiteSpace(query))
        return suggestions;
    
    // Step through code
    var context = ExtractContextAtCursor(query, cursorPosition);
    // ...
}
```

### View Debug Messages

In SSMS, look for messages like:

```
TEATime extension initialized successfully
IntelliSenseProvider: Generated 5 suggestions in 45ms
```

## Uninstalling the Extension

1. **Remove Registry Entry**:
   - Open Registry Editor
   - Navigate to SSMS Packages key
   - Delete your extension's GUID key

2. **Delete DLL Files**:
   - Remove the compiled DLL file

3. **Restart SSMS**:
   - Close all SSMS instances
   - Reopen SSMS (should no longer load the extension)

## Packaging for Distribution

When ready to release:

### Option 1: Create VSIX Package

1. Create a new VSIX Project in Visual Studio
2. Add your TEATime assembly as a reference
3. Configure manifest.xml with:
   - Extension name
   - Version
   - Description
   - Author info

4. Build the VSIX
5. Distribute the `.vsix` file

Users can install by:
- Double-clicking the .vsix file
- Or Tools → Extensions and Updates → Install from File

### Option 2: Registry-based Distribution

Create an installer (MSI) that:
1. Copies DLL to Program Files
2. Creates registry entries
3. Provides uninstall mechanism

## Performance Optimization

After setup, optimize performance:

1. **Adjust Cache TTL**:
   - In DatabaseMetadataCache.cs
   - Increase `CacheExpirationMinutes` for slower systems

2. **Limit Suggestion Count**:
   - In IntelliSenseProvider.cs
   - Reduce `.Take(25)` if performance is poor

3. **Async Loading**:
   - Implement async/await for database queries
   - Prevent UI freezing

## Troubleshooting Checklist

- [ ] SSMS version 18.0 or later installed
- [ ] SSMS DLLs are accessible and correct version
- [ ] Project builds without errors
- [ ] Registry entries created correctly
- [ ] GUID in code matches registry GUID
- [ ] DLL path in registry is correct (not a relative path)
- [ ] No other extensions conflicting
- [ ] Sufficient disk space for DLL
- [ ] Windows user has admin rights
- [ ] UAC (User Account Control) not blocking access

## Next Steps

1. Run the unit tests: `dotnet test`
2. Review DEVELOPER_GUIDE.md for architecture details
3. Check INTEGRATION_GUIDE.md for editor integration
4. Implement additional features as needed
5. Deploy to production once tested

## Additional Resources

- [SSMS Extensibility Documentation](https://docs.microsoft.com/en-us/sql/ssms/extensions)
- [Visual Studio SDK](https://docs.microsoft.com/visualstudio/extensibility/)
- [T-SQL Language Reference](https://docs.microsoft.com/sql/t-sql/language-reference)
- [SQL Server Management Objects (SMO)](https://docs.microsoft.com/sql/relational-databases/server-management-objects-smo)

## Getting Help

If you encounter issues:

1. Check this guide for solutions
2. Review error messages carefully
3. Check the GitHub Issues page
4. Create a detailed issue report with:
   - SSMS version
   - Visual Studio version
   - Full error message
   - Steps to reproduce

---

**Happy coding! 🚀**
