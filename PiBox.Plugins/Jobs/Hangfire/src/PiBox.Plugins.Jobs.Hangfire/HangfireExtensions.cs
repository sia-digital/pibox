using Microsoft.Extensions.DependencyInjection;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public static class HangfireExtensions
    {
        public static void ConfigureJobs(this IServiceCollection serviceCollection, Action<IJobManager, IServiceProvider> configure)
        {
            serviceCollection.AddSingleton(new JobOptions(configure));
        }
    }
}
