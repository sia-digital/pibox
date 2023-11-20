namespace PiBox.Plugins.Jobs.Hangfire.Job
{
    public interface IJobDetailsCollection : IList<JobDetails>, IJobRegister
    {
    }
}
