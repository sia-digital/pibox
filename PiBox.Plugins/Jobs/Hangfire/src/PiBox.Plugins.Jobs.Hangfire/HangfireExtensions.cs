using Microsoft.Extensions.DependencyInjection;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public static class HangfireExtensions
    {
        public static void ConfigureJobs(this IServiceCollection serviceCollection, Action<IJobRegister, IServiceProvider> configure)
        {
            serviceCollection.AddSingleton(new JobOptions(configure));
        }
    }
}
