using Hangfire;
using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Plugins.Jobs.Hangfire
{
    public interface IHangfireConfigurator : IPluginConfigurator
    {
        public bool IncludesStorage { get; }
        void Configure(IGlobalConfiguration config);
        void ConfigureServer(BackgroundJobServerOptions options);
    }
}
