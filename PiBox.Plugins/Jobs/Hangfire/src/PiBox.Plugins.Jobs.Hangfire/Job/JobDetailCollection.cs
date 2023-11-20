using Hangfire;

namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public class JobDetailCollection : List<JobDetails>, IJobDetailsCollection
    {
        public TimeZoneInfo DefaultTimeZoneInfo { get; set; } = TimeZoneInfo.Utc;
        public TimeSpan? DefaultTimeout { get; set; }

        public IJobRegisterBuilder RegisterRecurringAsyncJob<T>(string cronExpression) where T : IAsyncJob
        {
            var jobId = GetJobName<T>();

            var options = new JobDetails();
            options.Name = jobId;
            options.Timeout = DefaultTimeout;
            options.TimeZoneInfo = DefaultTimeZoneInfo;
            options.CronExpression = cronExpression;
            options.JobType = typeof(T);
            options.JobRegistration = () =>
                RecurringJob.AddOrUpdate<T>(jobId, x => x.ExecuteAsync(CancellationToken.None),
                    options.CronExpression, new RecurringJobOptions { TimeZone = options.TimeZoneInfo });
            Add(options);
            return new JobRegistrationBuilder(options);
        }

        public IJobRegisterBuilder RegisterParameterizedRecurringAsyncJob<TJob, TJobParams>(string cronExpression, TJobParams parameters,
            string jobSuffix = "") where TJob : IParameterizedAsyncJob<TJobParams>
        {
            var jobId = GetJobName<TJob, TJobParams>(parameters, jobSuffix);

            var options = new JobDetails();
            options.Name = jobId;
            options.Timeout = DefaultTimeout;
            options.TimeZoneInfo = DefaultTimeZoneInfo;
            options.CronExpression = cronExpression;
            options.JobType = typeof(TJob);
            options.JobParameter = parameters;
            options.JobRegistration = () => RecurringJob.AddOrUpdate<TJob>(jobId,
                x => x.ExecuteAsync(parameters, CancellationToken.None), options.CronExpression,
                new RecurringJobOptions { TimeZone = options.TimeZoneInfo });
            Add(options);
            return new JobRegistrationBuilder(options);
        }

        public void ActivateJobs()
        {
            foreach (var jobOptions in this.Where(x => x.JobRegistration != null))
            {
                jobOptions.JobRegistration!();
            }
        }

        private static string GetJobName<T>(string jobIdSuffix = "")
        {
            var name = typeof(T).Name;
            if (name.EndsWith("Job", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 3);
            }

            if (name.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 5);
            }

            if (!string.IsNullOrEmpty(jobIdSuffix))
            {
                name += $"_{jobIdSuffix}";
            }

            return name;
        }

        private static string GetJobName<T, TJobParam>(TJobParam jobParam, string jobIdSuffix = "")
        {
            var suffix = string.Empty;
            if (!string.IsNullOrEmpty(jobIdSuffix))

            {
                suffix = jobIdSuffix;
            }
            else if (jobParam != null)
            {
                suffix = jobParam.ToString();
            }

            return GetJobName<T>(suffix!);
        }
    }
}
