using Database.Initialization;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Hangfire.Initialization
{
    public static class HangfireJobStorage
    {
        public static JobStorage GetJobStorage(string connectionString, bool intitializeDatabase = true, string schemaName = "HangFire")
        {
            JobStorage storage;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                storage = new MemoryStorage.MemoryStorage();
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = schemaName,
                    PrepareSchemaIfNecessary = intitializeDatabase
                };
                storage = new SQLiteStorage(connectionString, storageOptions);
            }
            else
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = schemaName,
                    PrepareSchemaIfNecessary = intitializeDatabase,
                    //QueuePollInterval = TimeSpan.FromSeconds(15), // Default value,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true,
                    EnableHeavyMigrations = true
                };
                storage = new SqlServerStorage(connectionString, storageOptions);
            }

            return storage;
        }

        public static JobStorage GetJobStorage(DbConnection existingConnection, bool intitializeDatabase = true, string schemaName = "HangFire")
        {
            if (existingConnection is SqliteConnection)
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = schemaName,
                    PrepareSchemaIfNecessary = intitializeDatabase
                };

                return new SQLiteStorage(existingConnection, storageOptions);
            }
            else if (existingConnection is SqlConnection)
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = schemaName,
                    PrepareSchemaIfNecessary = intitializeDatabase,
                    //QueuePollInterval = TimeSpan.FromSeconds(15), // Default value,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true,
                    EnableHeavyMigrations = true
                };
                return new SqlServerStorage(existingConnection, storageOptions);
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }

    }
}
