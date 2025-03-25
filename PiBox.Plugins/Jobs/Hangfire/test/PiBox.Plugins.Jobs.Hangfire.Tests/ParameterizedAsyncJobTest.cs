using Microsoft.Extensions.Logging;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class ParameterizedAsyncJobTest(ILogger logger) : ParameterizedAsyncJob<string>(logger)
    {
        protected override Task<object> ExecuteJobAsync(string value, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Run {Value}", value);
            return Task.FromResult<object>(value);
        }
    }
}
