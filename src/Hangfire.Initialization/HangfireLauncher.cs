using Database.Initialization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;

namespace Hangfire.Initialization
{
    public static class HangfireLauncher
    {
        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServerInMemory()
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "", true);
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServerInMemory(string serverName)
        {
            return StartHangfireServer(serverName, "");
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServerSQLiteInMemory(bool prepareSchemaIfNecessary = true)
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "DataSource=:memory:;", prepareSchemaIfNecessary);
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServerSQLiteInMemory(string serverName, bool prepareSchemaIfNecessary = true)
        {
            return StartHangfireServer(serverName, "DataSource=:memory:;", prepareSchemaIfNecessary);
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServer(string serverName, string connectionString, bool prepareSchemaIfNecessary = true)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };
            return StartHangfireServer(options, connectionString, prepareSchemaIfNecessary);
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServer(BackgroundJobServerOptions options, string connectionString, bool prepareSchemaIfNecessary)
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
                    PrepareSchemaIfNecessary = prepareSchemaIfNecessary
                };
                storage = new SQLiteStorage(connectionString, storageOptions);
            }
            else
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    PrepareSchemaIfNecessary = prepareSchemaIfNecessary
                };
                storage = new SqlServerStorage(connectionString, storageOptions);
            }

            return StartHangfireServer(options, storage);
        }
        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServer(string serverName, DbConnection existingConnection, bool prepareSchemaIfNecessary = true)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };
            return StartHangfireServer(options, existingConnection, prepareSchemaIfNecessary);
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServer(BackgroundJobServerOptions options, DbConnection existingConnection, bool prepareSchemaIfNecessary)
        {
            if (existingConnection is SqliteConnection)
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    PrepareSchemaIfNecessary = prepareSchemaIfNecessary
                };

                var storage = new SQLiteStorage(existingConnection, storageOptions);

                return StartHangfireServer(options, storage);
            }
            else if (existingConnection is SqlConnection)
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    PrepareSchemaIfNecessary = prepareSchemaIfNecessary
                };
                var storage = new SqlServerStorage(existingConnection, storageOptions);

                return StartHangfireServer(options, storage);
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }

        public static (BackgroundJobServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient) StartHangfireServer(BackgroundJobServerOptions options, JobStorage storage)
        {
            var filterProvider = JobFilterProviders.Providers;
            var activator = JobActivator.Current;

            var backgroundJobFactory = new BackgroundJobFactory(filterProvider);
            var performer = new BackgroundJobPerformer(filterProvider, activator);
            var backgroundJobStateChanger = new BackgroundJobStateChanger(filterProvider);
            IEnumerable<IBackgroundProcess> additionalProcesses = new List<IBackgroundProcess>();

            var server = new BackgroundJobServer(options, storage, additionalProcesses,
                options.FilterProvider ?? filterProvider,
                options.Activator ?? activator,
                backgroundJobFactory,
                performer,
                backgroundJobStateChanger);

            var recurringJobManager = new RecurringJobManager(storage, backgroundJobFactory);

            var backgroundJobClient = new BackgroundJobClient(storage, backgroundJobFactory, backgroundJobStateChanger);

            return (server, recurringJobManager, backgroundJobClient);
        }
    }
}
