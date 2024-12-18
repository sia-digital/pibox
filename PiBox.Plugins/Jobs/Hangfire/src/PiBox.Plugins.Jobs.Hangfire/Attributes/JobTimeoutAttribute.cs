namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JobTimeoutAttribute : Attribute
    {
        public JobTimeoutAttribute(int timeout, TimeUnit unit)
        {
            switch (unit)
            {
                case TimeUnit.Milliseconds:
                    Timeout = TimeSpan.FromMilliseconds(timeout);
                    break;
                case TimeUnit.Seconds:
                    Timeout = TimeSpan.FromSeconds(timeout);
                    break;
                case TimeUnit.Minutes:
                    Timeout = TimeSpan.FromMinutes(timeout);
                    break;
                case TimeUnit.Hours:
                    Timeout = TimeSpan.FromHours(timeout);
                    break;
                case TimeUnit.Days:
                    Timeout = TimeSpan.FromDays(timeout);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        public TimeSpan Timeout { get; }
    }

    public enum TimeUnit
    {
        Milliseconds,
        Seconds,
        Minutes,
        Hours,
        Days
    }
}
