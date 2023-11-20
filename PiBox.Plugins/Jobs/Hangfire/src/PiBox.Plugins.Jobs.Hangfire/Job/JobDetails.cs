namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public class JobDetails
    {
        public Type JobType { get; set; }
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public TimeSpan? Timeout { get; set; }
        internal Action JobRegistration { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
        public object JobParameter { get; set; }
    }
}
