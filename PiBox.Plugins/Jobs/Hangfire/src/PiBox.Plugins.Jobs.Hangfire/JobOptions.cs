namespace PiBox.Plugins.Jobs.Hangfire
{
    public class JobOptions
    {
        public JobOptions(Action<IJobManager, IServiceProvider> configureJobs) => ConfigureJobs = configureJobs;
        public Action<IJobManager, IServiceProvider> ConfigureJobs { get; }
    }
}
