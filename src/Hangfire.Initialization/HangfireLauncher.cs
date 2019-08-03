using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Hangfire.Initialization
{
    public static class HangfireLauncher
    {
        public static HangfireServerDetails StartHangfireServerInMemory(Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "", config);
        }

        public static HangfireServerDetails StartHangfireServerInMemory(string serverName, Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(serverName, "", config);
        }

        public static HangfireServerDetails StartHangfireServerSQLiteInMemory(Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(new BackgroundJobServerOptions(), "DataSource=:memory:;", config);
        }

        public static HangfireServerDetails StartHangfireServerSQLiteInMemory(string serverName, Action<HangfireLauncherOptions> config = null)
        {
            return StartHangfireServer(serverName, "DataSource=:memory:;", config);
        }

        public static HangfireServerDetails StartHangfireServer(string serverName, string connectionString, Action<HangfireLauncherOptions> config = null)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                //Queues = new string[] { serverName, EnqueuedState.DefaultQueue }
            };
            return StartHangfireServer(options, connectionString, config);
        }

        public static HangfireServerDetails StartHangfireServer(string serverName, DbConnection existingConnection, Action<HangfireLauncherOptions> config = null)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                //Queues = new string[] { serverName, EnqueuedState.DefaultQueue }
            };
            return StartHangfireServer(options, existingConnection, config);
        }

        public static HangfireServerDetails StartHangfireServer(string serverName, JobStorage storage, Action<HangfireLauncherOptions> config = null)
        {
            var options = new BackgroundJobServerOptions
            {
                ServerName = serverName,
                //Queues = new string[] { serverName, EnqueuedState.DefaultQueue }
            };
            return StartHangfireServer(options, storage, config);
        }

        public static HangfireServerDetails StartHangfireServer(BackgroundJobServerOptions options, JobStorage storage, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            return StartHangfireServer(options, storage, launcherOptions);
        }

        public static HangfireServerDetails StartHangfireServer(BackgroundJobServerOptions options, string connectionString, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            JobStorage storage = HangfireJobStorage.GetJobStorage(connectionString, storageOptions => {
                storageOptions.PrepareSchemaIfNecessary = launcherOptions.StorageOptions.PrepareSchemaIfNecessary;
                storageOptions.EnableHeavyMigrations = launcherOptions.StorageOptions.EnableHeavyMigrations;
                storageOptions.EnableLongPolling = launcherOptions.StorageOptions.EnableLongPolling;
                storageOptions.SchemaName = launcherOptions.StorageOptions.SchemaName;
                storageOptions.QueuePollInterval = launcherOptions.StorageOptions.QueuePollInterval;
                storageOptions.CommandBatchMaxTimeout = launcherOptions.StorageOptions.CommandBatchMaxTimeout;
                storageOptions.SlidingInvisibilityTimeout = launcherOptions.StorageOptions.SlidingInvisibilityTimeout;
                storageOptions.UseRecommendedIsolationLevel = launcherOptions.StorageOptions.UseRecommendedIsolationLevel;
                storageOptions.UsePageLocksOnDequeue = launcherOptions.StorageOptions.UsePageLocksOnDequeue;
                storageOptions.DisableGlobalLocks = launcherOptions.StorageOptions.DisableGlobalLocks;
            }).JobStorage;

            return StartHangfireServer(options, storage, launcherOptions);
        }

        public static HangfireServerDetails StartHangfireServer(BackgroundJobServerOptions options, DbConnection existingConnection, Action<HangfireLauncherOptions> config = null)
        {
            var launcherOptions = new HangfireLauncherOptions();
            if (config != null)
                config(launcherOptions);

            JobStorage storage = HangfireJobStorage.GetJobStorage(existingConnection, storageOptions => {
                storageOptions.PrepareSchemaIfNecessary = launcherOptions.StorageOptions.PrepareSchemaIfNecessary;
                storageOptions.EnableHeavyMigrations = launcherOptions.StorageOptions.EnableHeavyMigrations;
                storageOptions.EnableLongPolling = launcherOptions.StorageOptions.EnableLongPolling;
                storageOptions.SchemaName = launcherOptions.StorageOptions.SchemaName;
                storageOptions.QueuePollInterval = launcherOptions.StorageOptions.QueuePollInterval;
                storageOptions.CommandBatchMaxTimeout = launcherOptions.StorageOptions.CommandBatchMaxTimeout;
                storageOptions.SlidingInvisibilityTimeout = launcherOptions.StorageOptions.SlidingInvisibilityTimeout;
                storageOptions.UseRecommendedIsolationLevel = launcherOptions.StorageOptions.UseRecommendedIsolationLevel;
                storageOptions.UsePageLocksOnDequeue = launcherOptions.StorageOptions.UsePageLocksOnDequeue;
                storageOptions.DisableGlobalLocks = launcherOptions.StorageOptions.DisableGlobalLocks;
            });

            return StartHangfireServer(options, storage, launcherOptions);
        }

        public static HangfireServerDetails StartHangfireServer(BackgroundJobServerOptions options, JobStorage storage, HangfireLauncherOptions launcherOptions)
        {
            //Always create a queue with the server name.
            if (!string.IsNullOrEmpty(options.ServerName) && !options.Queues.Contains(options.ServerName))
            {
                var queues = options.Queues.ToList();
                queues.Insert(0, options.ServerName);
                options.Queues = queues.ToArray();
            }

            var additionalProcesses = launcherOptions?.AdditionalProcesses ?? Enumerable.Empty<IBackgroundProcess>();

            options.FilterProvider = launcherOptions?.FilterProvider ?? options.FilterProvider ?? JobFilterProviders.Providers;
            options.TimeZoneResolver = launcherOptions?.TimeZoneResolver ?? options.TimeZoneResolver ?? new DefaultTimeZoneResolver();
            options.Activator = launcherOptions?.Activator ?? options.Activator ?? JobActivator.Current;

            var serverDetails = new HangfireServerDetails(options, storage, additionalProcesses, launcherOptions?.ApplicationLifetime);

            return serverDetails;
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

        public JobStorageOptions StorageOptions { get; set; } = new JobStorageOptions();

    }
}
