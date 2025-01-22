using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class AsyncHangfireJob : HangfireJob
    {
        protected AsyncHangfireJob(ILogger logger) : base(logger)
        {
        }

        protected async Task<object> InternalExecuteAsync(Func<Task<object>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Job failed");
                throw;
            }
        }
    }
}
