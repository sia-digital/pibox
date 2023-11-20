using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class HangfireJob : IDisposable
    {
        protected readonly ILogger Logger;

        public IJobDetailsCollection JobOptionsCollection { get; set; }

        protected HangfireJob(ILogger logger)
        {
            Logger = logger;
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
