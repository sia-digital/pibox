namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public interface IJobRegisterBuilder
    {
        IJobRegisterBuilder UseTimezone(TimeZoneInfo timeZoneInfo);
        IJobRegisterBuilder UseTimeout(TimeSpan timeout);
    }
}
