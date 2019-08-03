using Hangfire;
using Hangfire.Common;
using Hangfire.Initialization;
using Hangfire.Server;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            GlobalConfiguration.Configuration.UseColouredConsoleLogProvider();

            //GlobalJobFilters.Filters.Add(new HangfireLoggerAttribute());

            using (var server = HangfireLauncher.StartHangfireServerInMemory("console", options => {

                var jobFilters = new JobFilterCollection();
                jobFilters.Add(new CaptureCultureAttribute());
                jobFilters.Add(new AutomaticRetryAttribute());
                jobFilters.Add(new StatisticsHistoryAttribute());
                jobFilters.Add(new ContinuationsSupportAttribute());
                jobFilters.Add(new HangfireLoggerAttribute());

                options.FilterProvider = new JobFilterProviderCollection(jobFilters, new JobFilterAttributeFilterProvider());
            }))
            {
                Console.WriteLine("Hangfire Server started. Press any key to exit...");

                var job = Job.FromExpression<HangfireJob>(j => j.Execute());
                var queue = new EnqueuedState("console");

                server.BackgroundJobClient.Create(job, queue);

                Console.ReadKey();
            }
        }
    }
}


