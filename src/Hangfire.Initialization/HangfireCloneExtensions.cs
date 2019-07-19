namespace Hangfire.Initialization
{
    public static class HangfireCloneExtensions
    {
        public static DashboardOptions Clone(this DashboardOptions options)
        {
            return new DashboardOptions()
            {
                AppPath = options.AppPath,
                Authorization = options.Authorization,
                IsReadOnlyFunc = options.IsReadOnlyFunc,
                StatsPollingInterval = options.StatsPollingInterval,
                DisplayStorageConnectionString = options.DisplayStorageConnectionString,
                DisplayNameFunc = options.DisplayNameFunc,
                IgnoreAntiforgeryToken = options.IgnoreAntiforgeryToken,
                TimeZoneResolver = options.TimeZoneResolver
            };
        }

        public static BackgroundJobServerOptions Clone(this BackgroundJobServerOptions options)
        {
            return new BackgroundJobServerOptions()
            {
                ServerName = options.ServerName,
                WorkerCount = options.WorkerCount,
                Queues = (string[])options.Queues?.Clone(),
                StopTimeout = options.StopTimeout,
                ShutdownTimeout = options.ShutdownTimeout,
                SchedulePollingInterval = options.SchedulePollingInterval,
                HeartbeatInterval = options.HeartbeatInterval,
                ServerCheckInterval = options.ServerCheckInterval,
                ServerTimeout = options.ServerTimeout,
                CancellationCheckInterval = options.CancellationCheckInterval,
                FilterProvider = options.FilterProvider,
                Activator = options.Activator,
                TimeZoneResolver = options.TimeZoneResolver,
                TaskScheduler = options.TaskScheduler
            };
        }
    }
}
