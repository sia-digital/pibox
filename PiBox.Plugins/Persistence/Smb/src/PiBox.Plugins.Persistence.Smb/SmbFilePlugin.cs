using Microsoft.Extensions.DependencyInjection;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.Smb
{
    public class SmbFilePlugin : IPluginServiceConfiguration
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISmbStorage, SmbStorage>();
        }
    }
}