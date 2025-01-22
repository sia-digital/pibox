namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public interface IParameterizedAsyncJob<in T> : IDisposable
    {
        Task<object> ExecuteAsync(T value, CancellationToken jobCancellationToken = default);
    }
}
