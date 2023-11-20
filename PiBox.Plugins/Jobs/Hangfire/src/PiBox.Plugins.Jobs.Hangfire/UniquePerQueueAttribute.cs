using Hangfire.Common;
using Hangfire.States;

namespace PiBox.Plugins.Jobs.Hangfire
{
    /// <summary>
    /// There can only be one job of the same type queued or in processing.
    /// </summary>
    public class UniquePerQueueAttribute : JobFilterAttribute, IElectStateFilter
    {
        public string Queue { get; set; }
        public bool CheckScheduledJobs { get; set; }
        public bool CheckRunningJobs { get; set; }

        public UniquePerQueueAttribute(string queue)
        {
            Queue = queue;
        }

        private IEnumerable<JobEntity> GetJobs(ElectStateContext context)
        {
            var monitoringApi = context.Storage.GetMonitoringApi();
            var jobs = new List<JobEntity>();
            foreach (var (key, value) in monitoringApi.EnqueuedJobs(Queue, 0, 500))
                jobs.Add(JobEntity.Parse(key, value.Job));
            if (CheckScheduledJobs)
                foreach (var (key, value) in monitoringApi.ScheduledJobs(0, 500))
                    jobs.Add(JobEntity.Parse(key, value.Job));

            if (!CheckRunningJobs) return jobs;

            foreach (var (key, value) in monitoringApi.ProcessingJobs(0, 500))
            {
                jobs.Add(JobEntity.Parse(key, value.Job));
            }

            return jobs;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is not EnqueuedState enqueuedState)
            {
                return;
            }

            enqueuedState.Queue = Queue;

            var job = context.BackgroundJob;
            var jobs = GetJobs(context);
            if (jobs.Any(x => x.Value.Method == job.Job.Method && x.Value.Args.SequenceEqual(job.Job.Args) && x.Id != job.Id))
            {
                context.CandidateState = new DeletedState { Reason = "Instance of the same job is already queued." };
            }
        }

        private sealed record JobEntity(string Id, global::Hangfire.Common.Job Value)
        {
            public static JobEntity Parse(string id, global::Hangfire.Common.Job job) => new(id, job);
        }
    }
}
