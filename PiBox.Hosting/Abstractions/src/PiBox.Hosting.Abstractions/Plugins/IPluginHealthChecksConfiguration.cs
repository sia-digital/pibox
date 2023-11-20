using Microsoft.Extensions.DependencyInjection;

namespace PiBox.Hosting.Abstractions.Plugins
{
    public interface IPluginHealthChecksConfiguration : IPluginActivateable
    {
        void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder);
    }
}
