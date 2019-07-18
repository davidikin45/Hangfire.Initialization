using Database.Initialization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Hangfire.Initialization
{
    public static class HangfireLauncher
    {
        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServerInMemory(Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "", config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServerInMemory(string serverName, Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(serverName, "", config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServerSQLiteInMemory(Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "DataSource=:memory:;", config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServerSQLiteInMemory(string serverName, Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(serverName, "DataSource=:memory:;", config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(string serverName, string connectionString, Action<HangfireLauncherOptions> config = null)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };
            return StartHangfireServer(options, connectionString, config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(string serverName, DbConnection existingConnection, Action<HangfireLauncherOptions> config = null)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };
            return StartHangfireServer(options, existingConnection, config);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(BackgroundJobServerOptions options, JobStorage storage, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            return StartHangfireServer(options, storage, launcherOptions);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(BackgroundJobServerOptions options, string connectionString, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            JobStorage storage;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                storage = new MemoryStorage.MemoryStorage();
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = launcherOptions.SchemaName,
                    PrepareSchemaIfNecessary = launcherOptions.PrepareSchemaIfNecessary
                };
                storage = new SQLiteStorage(connectionString, storageOptions);
            }
            else
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = launcherOptions.SchemaName,
                    PrepareSchemaIfNecessary = launcherOptions.PrepareSchemaIfNecessary
                };
                storage = new SqlServerStorage(connectionString, storageOptions);
            }

            return StartHangfireServer(options, storage, launcherOptions);
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(BackgroundJobServerOptions options, DbConnection existingConnection, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            if (existingConnection is SqliteConnection)
            {
                var storageOptions = new SQLiteStorageOptions()
                {
                    SchemaName = launcherOptions.SchemaName,
                    PrepareSchemaIfNecessary = launcherOptions.PrepareSchemaIfNecessary
                };

                var storage = new SQLiteStorage(existingConnection, storageOptions);

                return StartHangfireServer(options, storage, launcherOptions);
            }
            else if (existingConnection is SqlConnection)
            {
                var storageOptions = new SqlServerStorageOptions()
                {
                    SchemaName = launcherOptions.SchemaName,
                    PrepareSchemaIfNecessary = launcherOptions.PrepareSchemaIfNecessary
                };
                var storage = new SqlServerStorage(existingConnection, storageOptions);

                return StartHangfireServer(options, storage, launcherOptions);
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }

        public static (IBackgroundProcessingServer Server, IRecurringJobManager RecurringJobManager, IBackgroundJobClient BackgroundJobClient, JobStorage Storage) StartHangfireServer(BackgroundJobServerOptions options, JobStorage storage, HangfireLauncherOptions launcherOptions)
        {
            var additionalProcesses = launcherOptions?.AdditionalProcesses ?? Enumerable.Empty<IBackgroundProcess>();

            var filterProvider = launcherOptions?.FilterProvider ?? options.FilterProvider ?? JobFilterProviders.Providers;
            var timeZoneResolver = launcherOptions?.TimeZoneResolver ?? new DefaultTimeZoneResolver();
            var activator = launcherOptions?.Activator ?? options.Activator ?? JobActivator.Current;

            //var factory = new BackgroundJobFactory(filterProvider);
            //var performer = new BackgroundJobPerformer(filterProvider, activator, options.TaskScheduler);
            //var stateChanger = new BackgroundJobStateChanger(filterProvider);

            var server = new BackgroundJobServer(options, storage, additionalProcesses);
            //var server = new HangfireBackgroundJobServer(options, storage, additionalProcesses, null, null, factory, performer, stateChanger);

            if (launcherOptions?.ApplicationLifetime != null)
            {
                launcherOptions?.ApplicationLifetime.ApplicationStopping.Register(() => server.SendStop());
                launcherOptions?.ApplicationLifetime.ApplicationStopped.Register(() => server.Dispose());
            }

            var recurringJobManager = new RecurringJobManager(storage, filterProvider, timeZoneResolver);

            var backgroundJobClient = new BackgroundJobClient(storage, filterProvider);

            return (server, recurringJobManager, backgroundJobClient, storage);
        }

        public static IBackgroundJobClient GetDefaultBackgroundJobClient()
        {
            var clientFactoryProperty = typeof(BackgroundJob).GetProperties(BindingFlags.Instance |
                  BindingFlags.NonPublic |
                  BindingFlags.Public).Where(p => p.Name == "ClientFactory").First();

            Func<IBackgroundJobClient> clientFactoryFunc = (Func<IBackgroundJobClient>)clientFactoryProperty.GetValue(null, null);
            var backgroundJobClient = clientFactoryFunc();

            return backgroundJobClient;
        }
    }

    public class HangfireLauncherOptions
    {
        public IEnumerable<IBackgroundProcess> AdditionalProcesses { get; set; }
        public IJobFilterProvider FilterProvider { get; set; }
        public JobActivator Activator { get; set; }
        public IApplicationLifetime ApplicationLifetime { get; set; }

        public ITimeZoneResolver TimeZoneResolver { get; set; }

        public bool PrepareSchemaIfNecessary { get; set; } = true;

        public string SchemaName { get; set; } = "HangFire";
    }
}
