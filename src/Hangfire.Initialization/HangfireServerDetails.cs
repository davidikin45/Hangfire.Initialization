using Hangfire.Common;
using Hangfire.Server;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Initialization
{
    public class HangfireServerDetails : IBackgroundProcessingServer
    {
        public string ServerName { get; }
        public int WorkerCount { get; }
        public string[] Queues { get; }
        public TimeSpan StopTimeout { get; set; }
        public TimeSpan ShutdownTimeout { get; set; }
        public TimeSpan SchedulePollingInterval { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan ServerCheckInterval { get; set; }
        public TimeSpan ServerTimeout { get; set; }
        public TimeSpan CancellationCheckInterval { get; set; }

        public IBackgroundProcessingServer Server { get; }
        public IRecurringJobManager RecurringJobManager { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }
        public JobStorage Storage { get; }
        public IJobFilterProvider FilterProvider { get;}
        public JobActivator Activator { get; }

        public HangfireServerDetails(BackgroundJobServerOptions options, JobStorage storage, IEnumerable<IBackgroundProcess> additionalProcesses, IApplicationLifetime applicationLifetime = null)
        {
            Server = new BackgroundJobServer(options, storage, additionalProcesses);

            //Server Details
            ServerName = options.ServerName;
            WorkerCount = options.WorkerCount;
            Queues = options.Queues;
            StopTimeout = options.StopTimeout;
            ShutdownTimeout = options.ShutdownTimeout;
            SchedulePollingInterval = options.SchedulePollingInterval;
            HeartbeatInterval = options.HeartbeatInterval;
            ServerCheckInterval = options.ServerCheckInterval;
            ServerTimeout = options.ServerTimeout;
            CancellationCheckInterval = options.CancellationCheckInterval;

            FilterProvider = options.FilterProvider;
            Activator = options.Activator;

            if (applicationLifetime != null)
            {
                applicationLifetime.ApplicationStopping.Register(() => Server.SendStop());
                applicationLifetime.ApplicationStopped.Register(() => Server.Dispose());
            }

            RecurringJobManager = new RecurringJobManager(storage, options.FilterProvider, options.TimeZoneResolver);

            BackgroundJobClient = new BackgroundJobClient(storage, options.FilterProvider);
        }

        public void SendStop()
        {
            Server.SendStop();
        }

        public bool WaitForShutdown(TimeSpan timeout)
        {
           return Server.WaitForShutdown(timeout);
        }

        public Task WaitForShutdownAsync(CancellationToken cancellationToken)
        {
            return Server.WaitForShutdownAsync(cancellationToken);
        }

        public void Dispose()
        {
            Server.Dispose();
        }
    }
}