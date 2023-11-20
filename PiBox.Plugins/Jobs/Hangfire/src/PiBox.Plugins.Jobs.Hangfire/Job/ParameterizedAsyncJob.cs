using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public abstract class ParameterizedAsyncJob<T> : AsyncHangfireJob, IParameterizedAsyncJob<T>
    {
        protected ParameterizedAsyncJob(ILogger logger) : base(logger)
        {
        }

        public Task<object> ExecuteAsync(T value, CancellationToken jobCancellationToken)
        {
            return InternalExecuteAsync(async () =>
            {
                var timeout = JobOptionsCollection
                    ?.FirstOrDefault(x => x.JobType == GetType() && Equals(x.JobParameter, value))?.Timeout;
                if (timeout == null)
                {
                    return await ExecuteJobAsync(value, jobCancellationToken);
                }

                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);
                cts.CancelAfter(timeout.Value);
                return await ExecuteJobAsync(value, cts.Token);
            });
        }

        protected abstract Task<object> ExecuteJobAsync(T value, CancellationToken cancellationToken);
    }
}
