using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Plugins.Jobs.Hangfire.Attributes;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class HangfireJob : IDisposable
    {
        protected readonly ILogger Logger;
        protected readonly TimeSpan? Timeout;

        protected HangfireJob(ILogger logger)
        {
            Logger = logger;
            Timeout = GetType().GetAttribute<JobTimeoutAttribute>()?.Timeout;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
        }
    }
}
