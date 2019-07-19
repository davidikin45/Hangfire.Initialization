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
        public static (JobStorage JobStorage, DbConnection ExistingConnection) GetJobStorage(string connectionString, Action<JobStorageOptions> config = null)
        {
            var options = new JobStorageOptions();
            if (config != null)
                config(options);

            DbConnection existingConnection = null;
            JobStorage storage;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                storage = new MemoryStorage.MemoryStorage();
            }
            else if (ConnectionStringHelper.IsSQLiteInMemory(connectionString))
            {
                existingConnection = new SqliteConnection(connectionString);
                storage = GetJobStorage(existingConnection, config);
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = options.SchemaName,
                    PrepareSchemaIfNecessary = options.PrepareSchemaIfNecessary,
                    QueuePollInterval = options.QueuePollInterval
                };
                storage = new SQLiteStorage(connectionString, storageOptions);
            }
            else
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = options.SchemaName,
                    PrepareSchemaIfNecessary = options.PrepareSchemaIfNecessary,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = options.EnableLongPolling ? options.SlidingInvisibilityTimeout : new Nullable<TimeSpan>(),
                    QueuePollInterval = options.EnableLongPolling ? TimeSpan.Zero : options.QueuePollInterval,
                    UseRecommendedIsolationLevel = options.UseRecommendedIsolationLevel,
                    UsePageLocksOnDequeue = options.UsePageLocksOnDequeue,
                    DisableGlobalLocks = options.DisableGlobalLocks,
                    EnableHeavyMigrations = options.EnableHeavyMigrations
                };
                storage = new SqlServerStorage(connectionString, storageOptions);
            }

            return (storage, existingConnection);
        }

        public static JobStorage GetJobStorage(DbConnection existingConnection, Action<JobStorageOptions> config = null)
        {
            var options = new JobStorageOptions();
            if (config != null)
                config(options);

            if (existingConnection is SqliteConnection)
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = options.SchemaName,
                    PrepareSchemaIfNecessary = options.PrepareSchemaIfNecessary,
                    QueuePollInterval = options.QueuePollInterval
                };

                return new SQLiteStorage(existingConnection, storageOptions);
            }
            else if (existingConnection is SqlConnection)
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = options.SchemaName,
                    PrepareSchemaIfNecessary = options.PrepareSchemaIfNecessary,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = options.EnableLongPolling ? options.SlidingInvisibilityTimeout : new Nullable<TimeSpan>(),
                    QueuePollInterval = options.EnableLongPolling ? TimeSpan.Zero : options.QueuePollInterval,
                    UseRecommendedIsolationLevel = options.UseRecommendedIsolationLevel,
                    UsePageLocksOnDequeue = options.UsePageLocksOnDequeue,
                    DisableGlobalLocks = options.DisableGlobalLocks,
                    EnableHeavyMigrations = options.EnableHeavyMigrations
                };
                return new SqlServerStorage(existingConnection, storageOptions);
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }
    }


    public class JobStorageOptions
    {
        public bool PrepareSchemaIfNecessary { get; set; } = true;
        public bool EnableHeavyMigrations { get; set; } = true;
        public bool EnableLongPolling { get; set; } = false;
        public string SchemaName { get; set; } = "HangFire";
        public TimeSpan QueuePollInterval { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan? CommandBatchMaxTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan? SlidingInvisibilityTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public bool UseRecommendedIsolationLevel { get; set; } = true;
        public bool UsePageLocksOnDequeue { get; set; } = true;
        public bool DisableGlobalLocks { get; set; } = true;
    }
}
