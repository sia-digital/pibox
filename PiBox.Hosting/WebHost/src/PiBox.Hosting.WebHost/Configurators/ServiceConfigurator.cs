using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Chronos;
using Chronos.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.DependencyInjection;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Middlewares.Models;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Hosting.WebHost.Extensions;
using PiBox.Hosting.WebHost.Formatters;
using PiBox.Hosting.WebHost.Logging;
using Serilog.Core;
using Serilog.Events;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace PiBox.Hosting.WebHost.Configurators
{
    internal class ServiceConfigurator
    {
        private readonly ILogger _logger;
        private readonly IImplementationResolver _implementationResolver;
        private readonly IConfiguration _configuration;
        private readonly IServiceCollection _serviceCollection;

        public ServiceConfigurator(ILogger logger, IConfiguration configuration, IServiceCollection serviceCollection, IImplementationResolver implementationResolver)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceCollection = serviceCollection;
            _implementationResolver = implementationResolver;
        }

        public void Configure()
        {
            ConfigureDefaultServices();
            ConfigureWebServices();
            ConfigureHealthChecks();
            ConfigureMetrics();
            ConfigureTracing();
            ConfigurePlugins();
        }

        internal void ConfigurePlugins()
        {
            _implementationResolver
                .FindPlugins<IPluginServiceConfiguration>()
                .ForEach(x =>
                {
                    _logger.LogDebug("Configured service {PluginServices} order {Order}", x.Value.GetType().Name,
                        x.Key);
                    x.Value.ConfigureServices(_serviceCollection);
                });
        }

        internal void ConfigureHealthChecks()
        {
            var healthChecksBuilder = _serviceCollection.AddHealthChecks();

            // default health checks
            healthChecksBuilder.AddCheck("api", () => HealthCheckResult.Healthy(), tags: new[] { HealthCheckTag.Liveness.Value });
            healthChecksBuilder.AddDiskStorageHealthCheck(o => o.CheckAllDrives = true, "disk_space", tags: new[] { HealthCheckTag.Liveness.Value });
            healthChecksBuilder.AddProcessAllocatedMemoryHealthCheck(1024 * 2, "memory", tags: new[] { HealthCheckTag.Liveness.Value });

            // custom health checks (discovered by attributes)
            var healthChecks = (
                from healthCheck in _implementationResolver.FindTypes(f =>
                    f.Implements<IHealthCheck>() && f.HasAttribute<HealthCheckAttribute>())
                let healthCheckAttribute = healthCheck.GetAttribute<HealthCheckAttribute>()!
                select new HealthCheckRegistration(healthCheckAttribute.Name,
                    sp => (IHealthCheck)ActivatorUtilities.GetServiceOrCreateInstance(sp, healthCheck),
                    HealthStatus.Unhealthy, healthCheckAttribute.Tags.Select(x => x.Value))).ToList();
            healthChecks.ForEach(hcr =>
            {
                _logger.LogInformation("Registering {HealthCheck} health check", hcr.Name);
                healthChecksBuilder.Add(hcr);
            });

            // health checks from plugins
            _implementationResolver.FindPlugins<IPluginHealthChecksConfiguration>()
                .ForEach(x =>
                {
                    _logger.LogDebug("Configured health checks {PluginHealthChecks} order {Order}",
                        x.Value.GetType().Name, x.Key);
                    x.Value.ConfigureHealthChecks(healthChecksBuilder);
                });
        }

        internal void ConfigureMetrics()
        {
            // add metrics
            _serviceCollection.AddOpenTelemetry().WithMetrics(
                builder =>
                {
                    var entryAssembly = Assembly.GetEntryAssembly();
                    var serviceName = entryAssembly!.GetName().Name ?? "unknown-service";
                    Baggage.SetBaggage("AppName", serviceName);

                    builder
                        // default labels/attributes -> this currently does not work because the spec is not approved yet https://github.com/open-telemetry/opentelemetry-dotnet/discussions/3333#discussioncomment-2907442
                        // .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(entryAssembly.GetName().Name,
                        //     serviceVersion: entryAssembly.GetName().Version?.ToString() ?? "unknown", serviceInstanceId: Environment.MachineName))
                        .ConfigureResource(resource =>
                        {
                            resource.AddService(serviceName,
                                serviceVersion: entryAssembly.GetName().Version?.ToString() ?? "unknown", serviceInstanceId: Environment.MachineName);
                        })
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Enrich = (string metricName, HttpContext context, ref TagList tags) =>
                            {
                                var authorizedParty = context.User.Claims.SingleOrDefault(x => x.Type == "azp")?.Value;
                                tags.Add("azp", authorizedParty);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddMeter("*")
                        .AddOtlpExporter()
                        .AddPrometheusExporter();
                });
            _serviceCollection.AddSingleton<ILogEventSink, LoggingMetricSink>();
        }

        internal void ConfigureTracing()
        {
            var tracingHost = _configuration["tracing:host"];
            if (!string.IsNullOrEmpty(tracingHost))
            {
                _serviceCollection.AddSingleton<ILogEventEnricher, OpentelemetryTraceEnricher>();
                var entryAssembly = Assembly.GetEntryAssembly();
                _serviceCollection.AddOpenTelemetry().WithTracing(
                    (builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(entryAssembly!.GetName().Name!))
                        .AddSource("*")
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation(instrumentationOptions => instrumentationOptions.Filter = (context) => !context.Request.Path.StartsWithSegments("/swagger"))
                        .AddOtlpExporter(o =>
                        {
                            _logger.LogInformation("Sending traces to {TracingHost}", tracingHost);
                            o.Endpoint = new Uri(tracingHost);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        })
                );
            }

            // Add ActivityListener to ActivitySource to enforce activitySource.StartActivity return non-null activities
            // see https://github.com/dotnet/runtime/issues/45070
            // must be done regardless if tracing is used or not otherwise there will be NREs
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(activityListener);
        }

        internal void ConfigureWebServices()
        {
            _serviceCollection.AddSingleton<GlobalStatusCodeOptions>();
            _serviceCollection.AddHttpContextAccessor();

            var corsPolicy = _configuration.BindToSection<CorsPolicy>(PiBoxWebHostDefaults.CorsConfigSectionName);
            corsPolicy.SetSanityDefaults();
            _serviceCollection.AddCors(opts => opts.AddPolicy(PiBoxWebHostDefaults.CorsPolicyName, corsPolicy));

            _serviceCollection.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = new[] { "application/json" };
            });
            _serviceCollection.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
            var mcvBuilder = _serviceCollection.AddControllers(options =>
                {
                    options.InputFormatters.Add(new YamlInputFormatter());
                    options.OutputFormatters.Add(new YamlOutputFormatter());
                    options.FormatterMappings.SetMediaTypeMappingForFormat("yaml", CustomMediaTypes.ApplicationYaml);
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = WriteValidationErrorResponse;
                });
            _implementationResolver.FindPlugins<IPluginControllerConfiguration>()
                .ForEach(pluginKeyPair =>
                {
                    _logger.LogDebug("Configured controller {PluginServices} order {Order}", pluginKeyPair.Value.GetType().Name,
                        pluginKeyPair.Key);
                    pluginKeyPair.Value.ConfigureControllers(mcvBuilder);
                });
            _serviceCollection.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
        }

        internal void ConfigureDefaultServices()
        {
            var logLevel = _configuration.GetValue<LogEventLevel?>("serilog:minimumLevel") ?? LogEventLevel.Information;
            _serviceCollection.WithLogLevelSwitch(logLevel);

            _serviceCollection.AddTransient(typeof(IFactory<>), typeof(Factory<>));

            _serviceCollection.AddDateTimeProvider();
            _serviceCollection.AddDateTimeOffsetProvider();

            _serviceCollection.AddMemoryCache();

            _serviceCollection.Configure<IpRateLimitOptions>(_configuration.GetSection("ipRateLimiting"));
            _serviceCollection.Configure<IpRateLimitPolicies>(_configuration.GetSection("ipRateLimitPolicies"));

            // inject counter and rules stores
            _serviceCollection.AddInMemoryRateLimiting();
            _serviceCollection.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            _serviceCollection.Configure<JsonOptions>(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            foreach (var configuration in _implementationResolver.FindAndResolve(f => f.HasAttribute<ConfigurationAttribute>()))
            {
                _serviceCollection.AddSingleton(configuration!.GetType(), configuration!);
            }
        }

        internal static IActionResult WriteValidationErrorResponse(ActionContext actionContext)
        {
            var fieldValidationErrors = actionContext.ModelState
                .Where(x => x.Value is { ValidationState: ModelValidationState.Invalid, Errors.Count: > 0 })
                .Select(x =>
                {
                    var (propertyName, modelStateEntry) = x;
                    if (!modelStateEntry.Errors.Any())
                        return new FieldValidationError(propertyName, "");
                    var errorMessage = string.Join(Environment.NewLine, modelStateEntry!.Errors.Select(e =>
                    {
                        var msg = e.ErrorMessage;
                        if (string.IsNullOrEmpty(msg) && e.Exception is Vogen.ValueObjectValidationException valueObjectValidationException)
                            return valueObjectValidationException.Message;
                        return msg;
                    }));
                    return new FieldValidationError(propertyName, errorMessage);
                });
            var dateTimeProvider = actionContext.HttpContext.RequestServices.GetRequiredService<IDateTimeProvider>();
            var validationErrorResponse = new ValidationErrorResponse(dateTimeProvider.UtcNow,
                "One or more validations have failed.", actionContext.HttpContext.TraceIdentifier,
                fieldValidationErrors);
            return new BadRequestObjectResult(validationErrorResponse);
        }
    }
}
