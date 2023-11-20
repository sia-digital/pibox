namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public interface IAsyncJob : IDisposable
    {
        Task<object> ExecuteAsync(CancellationToken jobCancellationToken);
    }
}
