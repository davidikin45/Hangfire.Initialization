using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Initialization
{
    public class HangfireBackgroundJobServer : IBackgroundProcessingServer
    {
        private readonly ILog _logger = LogProvider.For<BackgroundJobServer>();
        private readonly BackgroundJobServerOptions _options;
        private readonly BackgroundProcessingServer _processingServer;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public HangfireBackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IJobFilterProvider filterProvider,
            [CanBeNull] JobActivator activator,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            _options = options;

            var processes = new List<IBackgroundProcessDispatcherBuilder>();
            processes.AddRange(GetRequiredProcesses(filterProvider, activator, factory, performer, stateChanger));
            processes.AddRange(additionalProcesses.Select(x => x.UseBackgroundPool(1)));

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

            _logger.Info($"Starting Hangfire Server using job storage: '{storage}'");

            storage.WriteOptionsToLog(_logger);

            _logger.Info("Using the following options for Hangfire Server:\r\n" +
                $"    Worker count: {options.WorkerCount}\r\n" +
                $"    Listening queues: {String.Join(", ", options.Queues.Select(x => "'" + x + "'"))}\r\n" +
                $"    Shutdown timeout: {options.ShutdownTimeout}\r\n" +
                $"    Schedule polling interval: {options.SchedulePollingInterval}");

            _processingServer = new BackgroundProcessingServer(
                storage,
                processes,
                properties,
                GetProcessingServerOptions());
        }

        public void Dispose()
        {
            _processingServer.Dispose();
        }

        public void SendStop()
        {
            _logger.Debug("Hangfire Server is stopping...");
            _processingServer.SendStop();
        }

        public bool WaitForShutdown(TimeSpan timeout)
        {
            return _processingServer.WaitForShutdown(timeout);
        }

        public Task WaitForShutdownAsync(CancellationToken cancellationToken)
        {
            return _processingServer.WaitForShutdownAsync(cancellationToken);
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetRequiredProcesses(
           [CanBeNull] IJobFilterProvider filterProvider,
           [CanBeNull] JobActivator activator,
           [CanBeNull] IBackgroundJobFactory factory,
           [CanBeNull] IBackgroundJobPerformer performer,
           [CanBeNull] IBackgroundJobStateChanger stateChanger)
        {
            var processes = new List<IBackgroundProcessDispatcherBuilder>();
            var timeZoneResolver = _options.TimeZoneResolver ?? new DefaultTimeZoneResolver();

            if (factory == null && performer == null && stateChanger == null)
            {
                filterProvider = filterProvider ?? _options.FilterProvider ?? JobFilterProviders.Providers;
                activator = activator ?? _options.Activator ?? JobActivator.Current;

                factory = new BackgroundJobFactory(filterProvider);
                performer = new BackgroundJobPerformer(filterProvider, activator, _options.TaskScheduler);
                stateChanger = new BackgroundJobStateChanger(filterProvider);
            }
            else
            {
                if (factory == null) throw new ArgumentNullException(nameof(factory));
                if (performer == null) throw new ArgumentNullException(nameof(performer));
                if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));
            }

            processes.Add(new Worker(_options.Queues, performer, stateChanger).UseBackgroundPool(_options.WorkerCount));
            processes.Add(new DelayedJobScheduler(_options.SchedulePollingInterval, stateChanger).UseBackgroundPool(1));
            processes.Add(new RecurringJobScheduler(factory, _options.SchedulePollingInterval, timeZoneResolver).UseBackgroundPool(1));

            return processes;
        }

        private BackgroundProcessingServerOptions GetProcessingServerOptions()
        {
            return new BackgroundProcessingServerOptions
            {
                StopTimeout = _options.StopTimeout,
                ShutdownTimeout = _options.ShutdownTimeout,
                HeartbeatInterval = _options.HeartbeatInterval,
#pragma warning disable 618
                ServerCheckInterval = _options.ServerWatchdogOptions?.CheckInterval ?? _options.ServerCheckInterval,
                ServerTimeout = _options.ServerWatchdogOptions?.ServerTimeout ?? _options.ServerTimeout,
#pragma warning restore 618
                CancellationCheckInterval = _options.CancellationCheckInterval,
                ServerName = _options.ServerName
            };
        }
    }
}
