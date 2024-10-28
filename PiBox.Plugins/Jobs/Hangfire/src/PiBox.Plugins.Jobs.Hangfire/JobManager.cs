using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Plugins.Jobs.Hangfire.Extensions;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire
{
    internal class JobManager : IJobManager
    {
        private record PageOptions(int Offset, int PageSize)
        {
            public PageOptions Next() => this with { Offset = Offset + PageSize };
        }

        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly bool _hasQueueSupport;
        public IStorageConnection Connection { get; }
        public IMonitoringApi MonitoringApi { get; }

        public JobManager(bool hasQueueSupport,
            IStorageConnection connection,
            IMonitoringApi monitoringApi,
            IRecurringJobManager recurringJobManager,
            IBackgroundJobClient backgroundJobClient)
        {
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
            Connection = connection;
            MonitoringApi = monitoringApi;
            _hasQueueSupport = hasQueueSupport;
        }

        private static string GetJobName<T>(string jobIdSuffix = "")
        {
            var name = typeof(T).Name;
            if (name.EndsWith("Job", StringComparison.OrdinalIgnoreCase))
                name = name[..^3];
            if (name.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
                name = name[..^5];
            if (!string.IsNullOrEmpty(jobIdSuffix))
                name += $"_{jobIdSuffix}";
            return name;
        }

        public IList<RecurringJobDto> GetRecurringJobs() =>
            Connection.GetRecurringJobs();
        public IList<string> GetQueues() => MonitoringApi.Queues().Select(x => x.Name).ToList();

        public IList<EnqueuedJobDto> GetEnqueuedJobs() =>
            GetQueues()
                .SelectMany(queue => MonitoringApi.GetCompleteList((api, page) => api.EnqueuedJobs(queue, page.Offset, page.PageSize)))
                .Select(x => x.Value)
                .ToList();

        public IList<ProcessingJobDto> GetProcessingJobs() =>
            GetQueues()
                .SelectMany(queue => MonitoringApi.GetCompleteList((api, page) => api.ProcessingJobs(page.Offset, page.PageSize)))
                .Select(x => x.Value)
                .ToList();

        public IList<FailedJobDto> GetFailedJobs() =>
            MonitoringApi.GetCompleteList((api, page) => api.FailedJobs(page.Offset, page.PageSize))
                .Select(x => x.Value)
                .ToList();

        public IList<FetchedJobDto> GetFetchedJobs() =>
            GetQueues()
                .SelectMany(queue => MonitoringApi.GetCompleteList((api, page) => api.FetchedJobs(queue, page.Offset, page.PageSize)))
                .Select(x => x.Value)
                .ToList();

        public IList<global::Hangfire.Common.Job> GetJobs()
        {
            var jobs = GetEnqueuedJobs().Select(x => x.Job).ToList();
            jobs.AddRange(GetProcessingJobs().Select(x => x.Job));
            jobs.AddRange(GetFailedJobs().Select(x => x.Job));
            jobs.AddRange(GetFetchedJobs().Select(x => x.Job));
            jobs.AddRange(GetRecurringJobs().Select(x => x.Job));
            return jobs;
        }

        public IList<RecurringJobDto> GetRecurringJobs<T>(Predicate<RecurringJobDto> predicate = null) =>
            GetRecurringJobs().Where(x => x.Job.Type == typeof(T) && (predicate == null || predicate(x))).ToList();

        public IList<FetchedJobDto> GetFetchedJobs<T>(Predicate<FetchedJobDto> predicate = null) =>
            GetFetchedJobs().Where(x => x.Job.Type == typeof(T) && (predicate == null || predicate(x))).ToList();

        public IList<EnqueuedJobDto> GetEnqueuedJobs<T>(Predicate<EnqueuedJobDto> predicate = null) =>
            GetEnqueuedJobs().Where(x => x.Job.Type == typeof(T) && (predicate == null || predicate(x))).ToList();

        public IList<ProcessingJobDto> GetProcessingJobs<T>(Predicate<ProcessingJobDto> predicate = null) =>
            GetProcessingJobs().Where(x => x.Job.Type == typeof(T) && (predicate == null || predicate(x))).ToList();

        public IList<FailedJobDto> GetFailedJobs<T>(Predicate<FailedJobDto> predicate = null) =>
            GetFailedJobs().Where(x => x.Job.Type == typeof(T) && (predicate == null || predicate(x))).ToList();

        public string Enqueue<TJob, TJobParams>(TJobParams parameters, string queue = EnqueuedState.DefaultQueue)
            where TJob : IParameterizedAsyncJob<TJobParams>
        {
            return _hasQueueSupport
                ? _backgroundJobClient.Enqueue<TJob>(queue, x => x.ExecuteAsync(parameters, CancellationToken.None))
                : _backgroundJobClient.Enqueue<TJob>(x => x.ExecuteAsync(parameters, CancellationToken.None));
        }

        public string Enqueue<TJob>(string queue = EnqueuedState.DefaultQueue) where TJob : IAsyncJob
        {
            return _hasQueueSupport
                ? _backgroundJobClient.Enqueue<TJob>(x => x.ExecuteAsync(CancellationToken.None))
                : _backgroundJobClient.Enqueue<TJob>(queue, x => x.ExecuteAsync(CancellationToken.None));
        }

        public string Schedule<TJob>(TimeSpan schedule, string queue = EnqueuedState.DefaultQueue)
            where TJob : IAsyncJob
        {
            return _hasQueueSupport
                ? _backgroundJobClient.Schedule<TJob>(queue, x => x.ExecuteAsync(CancellationToken.None), schedule)
                : _backgroundJobClient.Schedule<TJob>(x => x.ExecuteAsync(CancellationToken.None), schedule);
        }

        public string Schedule<TJob, TJobParams>(TJobParams parameters, TimeSpan schedule,
            string queue = EnqueuedState.DefaultQueue) where TJob : IParameterizedAsyncJob<TJobParams>
        {
            return _hasQueueSupport
                ? _backgroundJobClient.Schedule<TJob>(queue, x => x.ExecuteAsync(parameters, CancellationToken.None), schedule)
                : _backgroundJobClient.Schedule<TJob>(x => x.ExecuteAsync(parameters, CancellationToken.None), schedule);
        }

        public void RegisterRecurring<TJob>(string cron, string queue = EnqueuedState.DefaultQueue)
            where TJob : IAsyncJob
        {
            if (_hasQueueSupport)
                _recurringJobManager.AddOrUpdate<TJob>(GetJobName<TJob>(), queue, x => x.ExecuteAsync(CancellationToken.None), cron, new RecurringJobOptions { TimeZone = GetTimeZone<TJob>() });
            else
                _recurringJobManager.AddOrUpdate<TJob>(GetJobName<TJob>(), x => x.ExecuteAsync(CancellationToken.None), cron, new RecurringJobOptions { TimeZone = GetTimeZone<TJob>() });
        }

        public void RegisterRecurring<TJob, TJobParams>(string cron, TJobParams parameters,
            string queue = EnqueuedState.DefaultQueue) where TJob : IParameterizedAsyncJob<TJobParams>
        {
            if (_hasQueueSupport)
                _recurringJobManager.AddOrUpdate<TJob>(GetJobName<TJob>(), queue, x => x.ExecuteAsync(parameters, CancellationToken.None), cron, new RecurringJobOptions { TimeZone = GetTimeZone<TJob>() });
            else
                _recurringJobManager.AddOrUpdate<TJob>(GetJobName<TJob>(), x => x.ExecuteAsync(parameters, CancellationToken.None), cron, new RecurringJobOptions { TimeZone = GetTimeZone<TJob>() });
        }

        public void DeleteRecurring(string id) => _recurringJobManager.RemoveIfExists(id);

        public void Delete(string id) => _backgroundJobClient.Delete(id);

        private static TimeZoneInfo GetTimeZone<TJob>()
        {
            return typeof(TJob).GetAttribute<JobTimeZoneAttribute>()?.TimeZoneInfo ?? TimeZoneInfo.Utc;
        }
    }
}
