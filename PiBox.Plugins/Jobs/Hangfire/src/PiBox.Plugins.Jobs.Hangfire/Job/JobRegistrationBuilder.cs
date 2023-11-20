namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public class JobRegistrationBuilder : IJobRegisterBuilder
    {
        private readonly JobDetails _jobDetails;

        public JobRegistrationBuilder(JobDetails details)
        {
            _jobDetails = details;
        }

        public IJobRegisterBuilder UseTimezone(TimeZoneInfo timeZoneInfo)
        {
            _jobDetails.TimeZoneInfo = timeZoneInfo;
            return this;
        }

        public IJobRegisterBuilder UseTimeout(TimeSpan timeout)
        {
            _jobDetails.Timeout = timeout;
            return this;
        }
    }
}
