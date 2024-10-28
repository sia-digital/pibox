using Hangfire;
using Hangfire.MemoryStorage;
using PiBox.Plugins.Jobs.Hangfire;

namespace PiBox.Example.Service
{
    public class TestConfigurator : IHangfireConfigurator
    {
        public bool IncludesStorage => false;

        public void Configure(IGlobalConfiguration config)
        {
            config.UseMemoryStorage();
        }

        public void ConfigureServer(BackgroundJobServerOptions options)
        {
            options.WorkerCount = 1;
        }
    }
}
