using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public class JobOptions
    {
        public JobOptions(Action<IJobRegister, IServiceProvider> configureJobs) => ConfigureJobs = configureJobs;
        public Action<IJobRegister, IServiceProvider> ConfigureJobs { get; }
    }
}
