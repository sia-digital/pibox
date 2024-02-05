using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    public class UniquePerQueueAttribute : JobFilterAttribute, IElectStateFilter
    {
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
            IMonitoringApi monitoringApi = context.Storage.GetMonitoringApi();
            List<JobEntity> jobs =
                new List<JobEntity>();
            foreach ((string key, EnqueuedJobDto enqueuedJobDto1) in monitoringApi.EnqueuedJobs(Queue, 0, 500))
            {
                string id = key;
                EnqueuedJobDto enqueuedJobDto2 = enqueuedJobDto1;
                jobs.Add(JobEntity.Parse(id, enqueuedJobDto2.Job));
            }

            if (CheckScheduledJobs)
            {
                foreach (KeyValuePair<string, ScheduledJobDto> pair in monitoringApi.ScheduledJobs(0, 500))
                {
                    string id = pair.Key;
                    ScheduledJobDto scheduledJobDto3 = pair.Value;
                    jobs.Add(JobEntity.Parse(id, scheduledJobDto3.Job));
                }
            }

            if (!CheckRunningJobs)
            {
                return jobs;
            }

            foreach (KeyValuePair<string, ProcessingJobDto> pair in
                     monitoringApi.ProcessingJobs(0, 500))
            {
                string id = pair.Key;
                ProcessingJobDto processingJobDto3 = pair.Value;
                jobs.Add(JobEntity.Parse(id, processingJobDto3.Job));
            }

            return jobs;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (!(context.CandidateState is EnqueuedState candidateState))
            {
                return;
            }

            candidateState.Queue = Queue;
            BackgroundJob job = context.BackgroundJob;
            var filteredArguments = job.Job.Args.Where(x => x.GetType() != typeof(CancellationToken)).ToList();
            var jobArgs = JsonSerializer.Serialize(filteredArguments,
                new JsonSerializerOptions() { IncludeFields = false });
            var jobs = GetJobs(context);
            var jobsWithArgs = jobs
                .Select(x => new { JobEntity = x, ArgAsString = jobArgs }).ToList();
            var alreadyExists = jobsWithArgs.Exists(x =>
                x.JobEntity.Value.Method == job.Job.Method && x.ArgAsString == jobArgs && x.JobEntity.Id != job.Id);
            if (!alreadyExists)
            {
                return;
            }

            context.CandidateState =
                new DeletedState() { Reason = "Instance of the same job is already queued." };
        }

        private sealed record JobEntity(string Id, global::Hangfire.Common.Job Value)
        {
            public static JobEntity
                Parse(string id, global::Hangfire.Common.Job job) =>
                new(id, job);
        }
    }
}
