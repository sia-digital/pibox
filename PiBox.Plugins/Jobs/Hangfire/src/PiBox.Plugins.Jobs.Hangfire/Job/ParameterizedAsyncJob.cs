using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class ParameterizedAsyncJob<T> : AsyncHangfireJob, IParameterizedAsyncJob<T>
    {
        protected ParameterizedAsyncJob(ILogger logger) : base(logger)
        {
        }

        public Task<object> ExecuteAsync(T value, CancellationToken jobCancellationToken = default)
        {
            return InternalExecuteAsync(async () =>
            {
                if (Timeout == null)
                {
                    return await ExecuteJobAsync(value, jobCancellationToken);
                }

                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);
                cts.CancelAfter(Timeout.Value);
                return await ExecuteJobAsync(value, cts.Token);
            });
        }

        protected abstract Task<object> ExecuteJobAsync(T value, CancellationToken cancellationToken);
    }
}
