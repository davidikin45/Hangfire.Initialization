using Database.Initialization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Hangfire.States;
using System.Collections.Generic;

namespace Hangfire.Initialization
{
    public static class HangfireLauncher
    {
        public static (BackgroundJobServer server, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient) StartHangfireServerInMemory()
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "");
        }

        public static (BackgroundJobServer server, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient) StartHangfireServerInMemory(string serverName)
        {
            return StartHangfireServer(serverName, "");
        }

        public static (BackgroundJobServer server, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient) StartHangfireServer(string serverName, string connectionString)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                Queues = new string[] { serverName, "default" }
            };
            return StartHangfireServer(options, connectionString);
        }

        public static (BackgroundJobServer server, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient) StartHangfireServer(BackgroundJobServerOptions options, string connectionString)
        {
            JobStorage storage;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                storage = new MemoryStorage.MemoryStorage();
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                storage = new SQLiteStorage(connectionString);
            }
            else
            {
                storage = new SqlServerStorage(connectionString);
            }

            var filterProvider = JobFilterProviders.Providers;
            var activator = JobActivator.Current;

            var backgroundJobFactory = new BackgroundJobFactory(filterProvider);
            var performer = new BackgroundJobPerformer(filterProvider, activator);
            var backgroundJobStateChanger = new BackgroundJobStateChanger(filterProvider);
            IEnumerable<IBackgroundProcess> additionalProcesses = null;

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
