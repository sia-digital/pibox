using Hangfire;
using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public interface IHangfireConfigurator : IPluginConfigurator
    {
        void Configure(IGlobalConfiguration config);
        void ConfigureServer(BackgroundJobServerOptions options);
    }
}
