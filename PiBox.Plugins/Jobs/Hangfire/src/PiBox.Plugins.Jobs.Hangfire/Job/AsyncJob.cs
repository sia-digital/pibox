using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class AsyncJob : AsyncHangfireJob, IAsyncJob
    {
        protected AsyncJob(ILogger logger) : base(logger)
        {
        }

        public Task<object> ExecuteAsync(CancellationToken jobCancellationToken)
        {
            return InternalExecuteAsync(async () =>
            {
                if (Timeout == null)
                {
                    return await ExecuteJobAsync(jobCancellationToken);
                }

                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);
                cts.CancelAfter(Timeout.Value);
                return await ExecuteJobAsync(cts.Token);
            });
        }

        protected abstract Task<object> ExecuteJobAsync(CancellationToken cancellationToken);
    }
}
