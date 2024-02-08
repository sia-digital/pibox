namespace PiBox.Plugins.Jobs.Hangfire.Attributes
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
