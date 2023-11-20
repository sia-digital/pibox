using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Hosting.WebHost.Logging;

namespace PiBox.Hosting.WebHost.Configurators
{
    internal class AppConfigurator
    {
        private readonly ILogger _logger;
        private readonly WebApplication _application;
        private readonly IServiceProvider _serviceProvider;
        private readonly IImplementationResolver _implementationResolver;

        public AppConfigurator(ILogger logger, WebApplication application, IImplementationResolver implementationResolver)
        {
            _logger = logger;
            _application = application;
            _serviceProvider = application.Services;
            _implementationResolver = implementationResolver;
        }

        public void Configure()
        {
            ConfigureWeb();
            ConfigureMiddlewares();
            ConfigurePlugins();
            ConfigureEndpoints();
        }

        internal void ConfigureEndpoints()
        {
#pragma warning disable ASP0014
            _application.UseEndpoints(endpoints =>
            {
                static Task ResponseWriter(HttpContext context, HealthReport report) => context.Response.WriteAsJsonAsync(new { report.Status, report.TotalDuration, Entries = report.Entries.Select(x => new { Name = x.Key, x.Value.Status, x.Value.Description, x.Value.Tags, x.Value.Duration, ExceptionMessage = x.Value.Exception?.Message }) });

                endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions { Predicate = p => p.Tags.Contains(HealthCheckTag.Liveness.Value), ResponseWriter = ResponseWriter });
                endpoints.MapHealthChecks("/health/readiness", new HealthCheckOptions { Predicate = p => p.Tags.Contains(HealthCheckTag.Readiness.Value), ResponseWriter = ResponseWriter });
                endpoints.MapControllers();
                _implementationResolver.FindPlugins<IPluginEndpointsConfiguration>()
                    .ForEach(x =>
                    {
                        _logger.LogDebug("Configured endpoints {PluginEndpoints} order {Order}", x.Value.GetType().Name,
                            x.Key);
                        x.Value.ConfigureEndpoints(_application, _serviceProvider);
                    });
            });
#pragma warning restore ASP0014
        }

        internal void ConfigurePlugins()
        {
            _implementationResolver.FindPlugins<IPluginApplicationConfiguration>()
                .ForEach(x =>
                {
                    _logger.LogDebug("Configured application {PluginApplications} order {Order}",
                        x.Value.GetType().Name, x.Key);
                    x.Value.ConfigureApplication(_application);
                });
        }

        internal void ConfigureMiddlewares()
        {
            var middlewares = _implementationResolver.FindTypes(f => f.Implements<ApiMiddleware>()).OrderBy(x =>
                    x.HasAttribute<MiddlewareAttribute>() ? x.GetAttribute<MiddlewareAttribute>()!.Order : int.MaxValue)
                .Select((type, i) => new { Order = i, Value = type });
            foreach (var middleware in middlewares)
            {
                _logger.LogDebug("Configured middleware {PluginMiddleware} order {Order}", middleware.Value.Name,
                    middleware.Order);
                _application.UseMiddleware(middleware.Value);
            }
        }

        private void ConfigureWeb()
        {
            _application.UseStructuredRequestLogging("/metrics", "/hangfire*", "/health/readiness", "/health/liveness", "/null");
            _application.UseForwardedHeaders();
            _application.UseResponseCompression();
            _application.UseIpRateLimiting();
            _application.UseCors(PiBoxWebHostDefaults.CorsPolicyName);
            _application.UseOpenTelemetryPrometheusScrapingEndpoint();
            _application.UseRouting();
        }
    }
}
