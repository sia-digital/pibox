using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Trace;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    public class EntityFrameworkPlugin(IImplementationResolver implementationResolver) : IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginHealthChecksConfiguration
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

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            var dbContexts = implementationResolver.FindAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(typeof(IDbContext)))
                .ToList();
            var registerHc = typeof(DependencyInjectionExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(IHealthChecksBuilder));

            foreach (var dbContext in dbContexts)
                registerHc.MakeGenericMethod(dbContext).Invoke(null, [healthChecksBuilder]);
        }
    }
}
