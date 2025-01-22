namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JobTimeZoneAttribute : Attribute
    {
        public JobTimeZoneAttribute(TimeZoneInfo timeZoneInfo)
        {
            TimeZoneInfo = timeZoneInfo;
        }

        public TimeZoneInfo TimeZoneInfo { get; }
    }
}
