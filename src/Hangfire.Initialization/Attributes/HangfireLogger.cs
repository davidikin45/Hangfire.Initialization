using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.Initialization
{
     //services.AddHangfire(config =>
     // {
     //       config.UseFilter(new HangfireLoggerAttribute());
     // });

    //http://docs.hangfire.io/en/latest/extensibility/using-job-filters.html
    public class HangfireLoggerAttribute : JobFilterAttribute,
    IServerFilter
    {
        private static ILog logger = LogProvider.GetLogger("Hangfire");

        public void OnPerforming(PerformingContext context)
        {

            logger.InfoFormat("Starting to perform job `{0}`", context.BackgroundJob.Id);
        }

        public void OnPerformed(PerformedContext context)
        {
            logger.InfoFormat("Job `{0}` has been performed", context.BackgroundJob.Id);
        }
    }
}
