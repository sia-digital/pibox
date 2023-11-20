using Microsoft.Extensions.Logging;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class ParameterizedAsyncJobTest : ParameterizedAsyncJob<string>
    {
        public ParameterizedAsyncJobTest(ILogger logger) : base(logger)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine();
            base.Dispose(disposing);
        }

        protected override Task<object> ExecuteJobAsync(string value, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Run {Value}", value);
            return Task.FromResult<object>(value);
        }
    }
}
