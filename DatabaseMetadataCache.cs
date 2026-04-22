using System;
using System.Collections.Generic;
using System.Linq;

namespace TEATime
{
    /// <summary>
    /// DatabaseMetadataCache
    /// 
    /// Caches metadata about database objects (tables, columns, schemas) for fast lookups.
    /// In a production system, this would query SQL Server's system views.
    /// For MVP, it includes basic caching and refresh mechanisms.
    /// </summary>
    public class DatabaseMetadataCache : IDisposable
    {
        private readonly Dictionary<string, List<TableMetadata>> _tableCache;
        private readonly Dictionary<string, List<string>> _columnCache;
        private readonly Dictionary<string, List<string>> _schemaCache;
        private readonly HashSet<string> _loadedDatabases;

        private const int CacheExpirationMinutes = 30;
        private Dictionary<string, DateTime> _cacheTimestamps;

        public DatabaseMetadataCache()
        {
            _tableCache = new Dictionary<string, List<TableMetadata>>();
            _columnCache = new Dictionary<string, List<string>>();
            _schemaCache = new Dictionary<string, List<string>>();
            _loadedDatabases = new HashSet<string>();
            _cacheTimestamps = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// Get all table names in a database
        /// </summary>
        public List<string> GetTables(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "master";

            EnsureCacheLoaded(databaseName);

            if (_tableCache.TryGetValue(databaseName, out var tables))
            {
                return tables.Select(t => $"{t.Schema}.{t.Name}").ToList();
            }

            return new List<string>();
        }

        /// <summary>
        /// Get all column names in a database
        /// </summary>
        public List<string> GetColumns(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "master";

            EnsureCacheLoaded(databaseName);

            if (_columnCache.TryGetValue(databaseName, out var columns))
            {
                return columns;
            }

            return new List<string>();
        }

        /// <summary>
        /// Get all schemas in a database
        /// </summary>
        public List<string> GetSchemas(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "master";

            EnsureCacheLoaded(databaseName);

            if (_schemaCache.TryGetValue(databaseName, out var schemas))
            {
                return schemas;
            }

            return new List<string>();
        }

        /// <summary>
        /// Get metadata for a specific table
        /// </summary>
        public TableMetadata GetTableMetadata(string databaseName, string tableName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "master";

            EnsureCacheLoaded(databaseName);

            if (_tableCache.TryGetValue(databaseName, out var tables))
            {
                return tables.FirstOrDefault(t => 
                    t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        /// <summary>
        /// Refresh the metadata cache for a specific database
        /// </summary>
        public void RefreshDatabaseMetadata(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                databaseName = "master";

            _loadedDatabases.Remove(databaseName);
            _tableCache.Remove(databaseName);
            _columnCache.Remove(databaseName);
            _schemaCache.Remove(databaseName);
            _cacheTimestamps.Remove(databaseName);

            EnsureCacheLoaded(databaseName);
        }

        /// <summary>
        /// Ensure cache is loaded and not expired for a database
        /// </summary>
        private void EnsureCacheLoaded(string databaseName)
        {
            // Check if cache is expired
            if (_cacheTimestamps.TryGetValue(databaseName, out var timestamp))
            {
                if (DateTime.UtcNow.Subtract(timestamp).TotalMinutes < CacheExpirationMinutes)
                {
                    return; // Cache is still valid
                }
            }

            // Load metadata
            LoadDatabaseMetadata(databaseName);
            _cacheTimestamps[databaseName] = DateTime.UtcNow;
        }

        /// <summary>
        /// Load metadata from the database
        /// 
        /// In a real implementation, this would query:
        /// - INFORMATION_SCHEMA.TABLES
        /// - INFORMATION_SCHEMA.COLUMNS
        /// - INFORMATION_SCHEMA.SCHEMATA
        /// 
        /// For MVP, we'll provide sample data that demonstrates the structure
        /// </summary>
        private void LoadDatabaseMetadata(string databaseName)
        {
            if (_loadedDatabases.Contains(databaseName))
                return;

            try
            {
                // This is MVP implementation with sample data
                // In production, connect to SQL Server using SMO or ADO.NET
                LoadSampleMetadata(databaseName);

                _loadedDatabases.Add(databaseName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading metadata for {databaseName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load sample metadata for demonstration
        /// Replace this with actual SQL Server queries
        /// </summary>
        private void LoadSampleMetadata(string databaseName)
        {
            var tables = new List<TableMetadata>
            {
                new TableMetadata
                {
                    Schema = "dbo",
                    Name = "Users",
                    Type = "TABLE",
                    Columns = new List<ColumnMetadata>
                    {
                        new ColumnMetadata { Name = "UserId", DataType = "int", IsPrimaryKey = true },
                        new ColumnMetadata { Name = "Username", DataType = "nvarchar(100)", IsNullable = false },
                        new ColumnMetadata { Name = "Email", DataType = "nvarchar(255)", IsNullable = false },
                        new ColumnMetadata { Name = "CreatedDate", DataType = "datetime", IsNullable = false }
                    }
                },
                new TableMetadata
                {
                    Schema = "dbo",
                    Name = "Orders",
                    Type = "TABLE",
                    Columns = new List<ColumnMetadata>
                    {
                        new ColumnMetadata { Name = "OrderId", DataType = "int", IsPrimaryKey = true },
                        new ColumnMetadata { Name = "UserId", DataType = "int", IsForeignKey = true },
                        new ColumnMetadata { Name = "OrderDate", DataType = "datetime", IsNullable = false },
                        new ColumnMetadata { Name = "TotalAmount", DataType = "decimal(10,2)", IsNullable = false }
                    }
                }
            };

            _tableCache[databaseName] = tables;

            // Build column list
            var columns = new List<string>();
            foreach (var table in tables)
            {
                columns.AddRange(table.Columns.Select(c => c.Name));
            }
            _columnCache[databaseName] = columns.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Build schema list
            var schemas = tables.Select(t => t.Schema).Distinct().ToList();
            _schemaCache[databaseName] = schemas;
        }

        public void Dispose()
        {
            _tableCache.Clear();
            _columnCache.Clear();
            _schemaCache.Clear();
            _loadedDatabases.Clear();
            _cacheTimestamps.Clear();
        }
    }
}
