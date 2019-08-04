using Hangfire;
using Hangfire.Common;
using Hangfire.Initialization;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        //https://github.com/HangfireIO/Hangfire/blob/a07ad0b9926923db75747d92796c5a9db39c1a87/samples/ConsoleSample/Program.cs
        static async Task Main(string[] args)
        {
            GlobalConfiguration.Configuration
            .UseColouredConsoleLogProvider()
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

            GlobalJobFilters.Filters.Add(new HangfireLoggerAttribute());
            GlobalJobFilters.Filters.Add(new HangfirePreserveOriginalQueueAttribute());

            using (var server = HangfireLauncher.StartHangfireServerInMemory("console", options =>
            {

                //var jobFilters = new JobFilterCollection();
                //jobFilters.Add(new CaptureCultureAttribute());
                //jobFilters.Add(new AutomaticRetryAttribute());
                //jobFilters.Add(new StatisticsHistoryAttribute());
                //jobFilters.Add(new ContinuationsSupportAttribute());

                //jobFilters.Add(new HangfireLoggerAttribute());
                //jobFilters.Add(new HangfirePreserveOriginalQueueAttribute());

                //options.FilterProvider = new JobFilterProviderCollection(jobFilters, new JobFilterAttributeFilterProvider());
            }))
            {
                Console.WriteLine("Hangfire Server started. Press any key to exit...");

                var job = Job.FromExpression<HangfireJob>(j => j.Execute());
                var queue = new EnqueuedState("console");

                server.BackgroundJobClient.Create(job, queue);

                server.RecurringJobManager.AddOrUpdate("seconds", () => Console.WriteLine("Hello, seconds!"), "*/15 * * * * *");
                server.RecurringJobManager.AddOrUpdate("minutely", () => Console.WriteLine("Hello, world!"), Cron.Minutely);
                server.RecurringJobManager.AddOrUpdate("hourly", () => Console.WriteLine("Hello"), "25 15 * * *");
                server.RecurringJobManager.AddOrUpdate("neverfires", () => Console.WriteLine("Can only be triggered"), "0 0 31 2 *");

                server.RecurringJobManager.AddOrUpdate("Hawaiian", () => Console.WriteLine("Hawaiian"), "15 08 * * *", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time"));
                server.RecurringJobManager.AddOrUpdate("UTC", () => Console.WriteLine("UTC"), "15 18 * * *");
                server.RecurringJobManager.AddOrUpdate("Russian", () => Console.WriteLine("Russian"), "15 21 * * *", TimeZoneInfo.Local);

                Console.ReadKey();
            }
        }
    }
}


