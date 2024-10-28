using System.Text.Json;
using Hangfire.Common;
using Hangfire.States;
using PiBox.Plugins.Jobs.Hangfire.Extensions;

namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    public class UniquePerQueueAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { IncludeFields = false };
        public string Queue { get; set; }

        public bool CheckScheduledJobs { get; set; }

        public bool CheckRunningJobs { get; set; }

        public UniquePerQueueAttribute(string queue)
        {
            Queue = queue;
            Order = 10;
        }

        private IEnumerable<JobEntity> GetJobs(ElectStateContext context)
        {
            var monitoringApi = context.Storage.GetMonitoringApi();
            var jobs = new List<JobEntity>();
            foreach (var (key, enqueuedJobDto1) in monitoringApi.GetCompleteList((api, page) => api.EnqueuedJobs(Queue, page.Offset, page.PageSize)))
                jobs.Add(JobEntity.Parse(key, enqueuedJobDto1.Job));

            if (CheckScheduledJobs)
                foreach (var (id, scheduledJobDto3) in monitoringApi.GetCompleteList((api, page) => api.ScheduledJobs(page.Offset, page.PageSize)))
                    jobs.Add(JobEntity.Parse(id, scheduledJobDto3.Job));

            if (!CheckRunningJobs)
                return jobs;

            foreach (var (id, processingJobDto3) in monitoringApi.GetCompleteList((api, page) => api.ProcessingJobs(page.Offset, page.PageSize)))
                jobs.Add(JobEntity.Parse(id, processingJobDto3.Job));
            return jobs;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is not EnqueuedState candidateState)
                return;

            candidateState.Queue = Queue;
            var job = context.BackgroundJob;
            var filteredArguments = job.Job.Args.Where(x => x.GetType() != typeof(CancellationToken)).ToList();
            var jobArgs = JsonSerializer.Serialize(filteredArguments, _jsonSerializerOptions);
            var jobs = GetJobs(context);
            var jobsWithArgs = jobs
                .Select(x => new { JobEntity = x, ArgAsString = jobArgs }).ToList();
            var alreadyExists = jobsWithArgs.Exists(x =>
                x.JobEntity.Value.Method == job.Job.Method && x.ArgAsString == jobArgs && x.JobEntity.Id != job.Id);
            if (!alreadyExists)
                return;

            context.CandidateState =
                new DeletedState { Reason = "Instance of the same job is already queued." };
        }

        private sealed record JobEntity(string Id, global::Hangfire.Common.Job Value)
        {
            public static JobEntity
                Parse(string id, global::Hangfire.Common.Job job) =>
                new(id, job);
        }
    }
}
