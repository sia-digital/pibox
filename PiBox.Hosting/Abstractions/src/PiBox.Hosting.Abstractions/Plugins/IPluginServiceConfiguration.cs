using Microsoft.Extensions.DependencyInjection;

namespace PiBox.Hosting.Abstractions.Plugins
{
    public interface IPluginServiceConfiguration : IPluginActivateable
    {
        void ConfigureServices(IServiceCollection serviceCollection);
    }
}
