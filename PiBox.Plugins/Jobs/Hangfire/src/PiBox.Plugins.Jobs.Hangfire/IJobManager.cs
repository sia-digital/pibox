using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public interface IJobManager
    {
        IStorageConnection Connection { get; }
        IMonitoringApi MonitoringApi { get; }
        IList<RecurringJobDto> GetRecurringJobs();
        IList<string> GetQueues();
        IList<EnqueuedJobDto> GetEnqueuedJobs();
        IList<ProcessingJobDto> GetProcessingJobs();
        IList<FailedJobDto> GetFailedJobs();
        IList<FetchedJobDto> GetFetchedJobs();
        IList<RecurringJobDto> GetRecurringJobs<T>(Predicate<RecurringJobDto> predicate = null);
        IList<FetchedJobDto> GetFetchedJobs<T>(Predicate<FetchedJobDto> predicate = null);
        IList<EnqueuedJobDto> GetEnqueuedJobs<T>(Predicate<EnqueuedJobDto> predicate = null);
        IList<ProcessingJobDto> GetProcessingJobs<T>(Predicate<ProcessingJobDto> predicate = null);
        IList<FailedJobDto> GetFailedJobs<T>(Predicate<FailedJobDto> predicate = null);
        string Enqueue<TJob, TJobParams>(TJobParams parameters, string queue = EnqueuedState.DefaultQueue) where TJob : IParameterizedAsyncJob<TJobParams>;
        string Enqueue<TJob>(string queue = EnqueuedState.DefaultQueue) where TJob : IAsyncJob;
        string Schedule<TJob>(TimeSpan schedule, string queue = EnqueuedState.DefaultQueue) where TJob : IAsyncJob;
        string Schedule<TJob, TJobParams>(TJobParams parameters, TimeSpan schedule, string queue = EnqueuedState.DefaultQueue) where TJob : IParameterizedAsyncJob<TJobParams>;
        void RegisterRecurring<TJob>(string cron, string queue = EnqueuedState.DefaultQueue) where TJob : IAsyncJob;
        void RegisterRecurring<TJob, TJobParams>(string cron, TJobParams parameters, string queue = EnqueuedState.DefaultQueue) where TJob : IParameterizedAsyncJob<TJobParams>;
        void DeleteRecurring(string id);
        void Delete(string id);
    }
}
