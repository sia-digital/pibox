namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public interface IJobRegister
    {
        TimeZoneInfo DefaultTimeZoneInfo { get; set; }
        public TimeSpan? DefaultTimeout { get; set; }

        IJobRegisterBuilder RegisterRecurringAsyncJob<T>(string cronExpression) where T : IAsyncJob;

        IJobRegisterBuilder RegisterParameterizedRecurringAsyncJob<TJob, TJobParams>(string cronExpression, TJobParams parameters,
            string jobSuffix = "") where TJob : IParameterizedAsyncJob<TJobParams>;

        void ActivateJobs();
    }
}
