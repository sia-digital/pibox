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
            object result;
            try
            {
                result = await action().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Job failed");
                throw;
            }

            return result;
        }
    }
}
