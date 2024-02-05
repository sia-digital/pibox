using Microsoft.Extensions.Logging;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    [RecurringJob("0 0 * * *")]
    public class TestJobAsync : AsyncJob
    {
        public TestJobAsync(ILogger logger) : base(logger)
        {
        }

        protected override async Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
        {
            const string result = "test";
            Logger.LogInformation("Run");
            await Task.Delay(100, cancellationToken);
            return result;
        }
    }

    public class JobFailsJob : AsyncJob
    {
        public JobFailsJob(ILogger logger) : base(logger)
        {
        }

        protected override Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
