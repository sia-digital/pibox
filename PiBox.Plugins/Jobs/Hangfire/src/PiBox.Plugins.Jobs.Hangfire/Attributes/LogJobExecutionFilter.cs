using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    public class LogJobExecutionFilter : JobFilterAttribute,
        IServerFilter
    {
        private readonly ILoggerFactory _loggerFactory;

        public LogJobExecutionFilter(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        public void OnPerforming(PerformingContext context)
        {
            _loggerFactory.CreateLogger(context.BackgroundJob.Job.Type)
                .LogInformation("Job with id {JobId} started executing", context.BackgroundJob.Id);
        }

        public void OnPerformed(PerformedContext context)
        {
            _loggerFactory.CreateLogger(context.BackgroundJob.Job.Type)
                .LogInformation("Job with id {JobId} finished executing", context.BackgroundJob.Id);
        }
    }
}
