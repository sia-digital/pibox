namespace PiBox.Plugins.Jobs.Hangfire
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RecurringJobAttribute : Attribute
    {
        public RecurringJobAttribute(string cronPattern)
        {
            CronPattern = cronPattern;
        }

        public string CronPattern { get; }
    }
}
