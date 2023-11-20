using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Trace;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    public class EntityFrameworkPlugin : IPluginServiceConfiguration, IPluginApplicationConfiguration
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(IRepository<>), typeof(EntityFrameworkRepository<>));
            serviceCollection.AddTransient(typeof(IReadRepository<>), typeof(EntityFrameworkRepository<>));
            serviceCollection.AddOpenTelemetry().WithTracing(builder => builder
                .AddEntityFrameworkCoreInstrumentation(EnrichEfCoreWithActivity));
        }

        internal static void EnrichEfCoreWithActivity(EntityFrameworkInstrumentationOptions options)
        {
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                var stateDisplayName = $"{command.CommandType} main";
                activity.DisplayName = stateDisplayName;
                activity.SetTag("db.name", stateDisplayName);
            };
        }

        public void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            DiagnosticListener.AllListeners.Subscribe(new DiagnosticObserver());
        }
    }
}
