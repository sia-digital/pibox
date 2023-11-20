using Microsoft.AspNetCore.Builder;

namespace PiBox.Hosting.Abstractions.Plugins
{
    public interface IPluginApplicationConfiguration : IPluginActivateable
    {
        void ConfigureApplication(IApplicationBuilder applicationBuilder);
    }
}
