using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PiBox.Hosting.WebHost.Logging
{
    /// <summary>
    /// Provides extension methods for structured logging.
    /// </summary>
    public static class StructuredLoggingExtensions
    {
        private const string NonContainerTemplate =
            "[{Timestamp:O} {Level:u3}] {SourceContext} => {Message:lj} {Properties:j} {NewLine}{Exception}";

        private static readonly ITextFormatter gelfJsonFormat = new GelfJsonFormatter();

        private static bool IsContainerProcess()
        {
            var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.ToLower(CultureInfo.InvariantCulture) ?? "false";
            return runningInContainer == "true";
        }

        /// <summary>
        /// Creates a simple bootstrap logger which should ONLY be used until the ioc container and other startup routines are loaded and finished.
        ///
        /// The example at the top of this page shows how to configure Serilog immediately when the application starts.
        /// This has the benefit of catching and reporting exceptions thrown during set-up of the ASP.NET Core host.
        /// The downside of initializing Serilog first is that services from the ASP.NET Core host,
        /// including the appsettings.json configuration and dependency injection, aren't available yet.
        /// To address this, Serilog supports two-stage initialization.
        /// An initial "bootstrap" logger is configured immediately when the program starts, and this is replaced by the fully-configured logger once the host has loaded.
        /// See https://github.com/serilog/serilog-aspnetcore#two-stage-initialization
        /// </summary>
        public static void CreateBootstrapLogger(LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel
                .Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext();

            if (IsContainerProcess())
            {
                loggerConfig.WriteTo.File("./logs/log.txt", outputTemplate: NonContainerTemplate,
                    rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 1073741824, formatProvider: CultureInfo.InvariantCulture);
                loggerConfig.WriteTo.Console(gelfJsonFormat);
            }
            else
            {
                loggerConfig.WriteTo.Console(outputTemplate: NonContainerTemplate, formatProvider: CultureInfo.InvariantCulture);
            }

            Log.Logger = loggerConfig.CreateBootstrapLogger();
        }

        /// <summary>
        /// Enables serilog enhanced request logging.
        ///
        /// It's important that the UseSerilogRequestLogging() call appears before handlers such as MVC.
        /// The middleware will not time or log components that appear before it in the pipeline.
        /// (This can be utilized to exclude noisy handlers from logging, such as UseStaticFiles(), by placing UseSerilogRequestLogging() after them.)
        /// See https://github.com/serilog/serilog-aspnetcore#request-logging
        /// </summary>
        /// <param name="app">The IApplicationBuilder instance.</param>
        /// <param name="configureOptions">The options to configure.</param>
        /// <returns>The IApplicationBuilder instance.</returns>
        [ExcludeFromCodeCoverage]
        public static IApplicationBuilder UseStructuredRequestLogging(this IApplicationBuilder app,
            Action<RequestLoggingOptions> configureOptions = null)
        {
            app.UseSerilogRequestLogging(configureOptions);
            return app;
        }

        /// <summary>
        /// Enables serilog enhanced request logging.
        ///
        /// It's important that the UseSerilogRequestLogging() call appears before handlers such as MVC.
        /// The middleware will not time or log components that appear before it in the pipeline.
        /// (This can be utilized to exclude noisy handlers from logging, such as UseStaticFiles(), by placing UseSerilogRequestLogging() after them.)
        /// See https://github.com/serilog/serilog-aspnetcore#request-logging
        /// </summary>
        /// <param name="app">The IApplicationBuilder instance.</param>
        /// <param name="requestPathsToExclude">Sets the log level to verbose for those request paths (case-insensitive, trailing wildcard * possible), so they should not be logged in production normally.</param>
        /// <returns>The IApplicationBuilder instance.</returns>
        [ExcludeFromCodeCoverage]
        public static IApplicationBuilder UseStructuredRequestLogging(this IApplicationBuilder app,
            params string[] requestPathsToExclude)
        {
            var normalizedPaths = requestPathsToExclude.Select(x => x.ToLowerInvariant().Trim());
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = DetermineRequestLogLevel(normalizedPaths);
                options.EnrichDiagnosticContext = (context, httpContext) =>
                {
                    var authorizedParty = httpContext.User.Claims.SingleOrDefault(x => x.Type == "azp")?.Value ?? string.Empty;
                    context.Set("azp", authorizedParty);
                };
            });
            return app;
        }

        /// <summary>
        /// Sets the log level to verbose for the given paths (case-insensitive, trailing wildcard * possible), so they should not be logged in production normally.
        /// </summary>
        /// <param name="normalizedPaths">the paths to set to log level verbose.</param>
        /// <returns>The delegate function which can be assigned to options.GetLevel of the serilog request logging options.</returns>
        internal static Func<HttpContext, double, Exception, LogEventLevel> DetermineRequestLogLevel(
            IEnumerable<string> normalizedPaths)
        {
            return (httpContext, elapsedMs, exception) =>
            {
                try
                {
                    var path = httpContext.Request.Path.Value.ToLowerInvariant().Trim();
                    if (normalizedPaths.Any(normalizedPath =>
                            (normalizedPath.EndsWith("*", StringComparison.OrdinalIgnoreCase) &&
                             path.StartsWith(normalizedPath.Replace("*", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase)) ||
                            normalizedPath == path))
                    {
                        return LogEventLevel.Verbose;
                    }

                    return exception != null || httpContext.Response.StatusCode > 499
                        ? LogEventLevel.Error
                        : LogEventLevel.Information;
                }
                catch
                {
                    // make sure we don't crash here
                    return LogEventLevel.Information;
                }
            };
        }

        /// <summary>
        /// Registers a singelton instance of LoggingLevelSwitch for later usage.
        ///
        /// If you need to change log level in run time with dependency injection:
        /// var loglevel = app.ApplicationServices.GetService&lt;LoggingLevelSwitch&gt;()
        /// loglevel.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
        ///
        /// Or via dependency injection in constructor
        /// public Constructor(LoggingLevelSwitch logLevelSwitch){
        ///    logLevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
        /// }
        /// </summary>
        /// <param name="serviceCollection">The IServiceCollection instance.</param>
        /// <param name="minimumLevel">The minimum log level which should be logged at the moment.</param>
        /// <returns>The IServiceCollection instance.</returns>
        [ExcludeFromCodeCoverage]
        public static IServiceCollection WithLogLevelSwitch(this IServiceCollection serviceCollection,
            LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            serviceCollection.AddSingleton(new LoggingLevelSwitch(minimumLevel));
            return serviceCollection;
        }

        /// <summary>
        /// Enables structured logging powered by serilog for all builds except DEBUG.
        /// The initial minimum loglevel is read from this ENV-VAR "SERILOG_MINIMUMLEVEL" if set, otherwise INFO.
        ///
        /// The UseStructuredLogging() call will configure the logging pipeline with any registered implementations of the following services:
        /// IDestructuringPolicy
        /// ILogEventEnricher
        /// ILogEventFilter
        /// ILogEventSink
        /// LoggingLevelSwitch
        /// See Serilog: https://github.com/serilog/serilog-aspnetcore#serilogaspnetcore
        /// </summary>
        /// <param name="hostBuilder">The IHostBuilder instance.</param>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="customLogEventLevels">Dictionary containing additional logLevels per namespace.</param>
        /// <returns>The IHostBuilder instance.</returns>
        public static IHostBuilder UseStructuredLogging(this IHostBuilder hostBuilder,
            LogEventLevel minimumLevel = LogEventLevel.Information,
            Dictionary<string, LogEventLevel> customLogEventLevels = null)
        {
            return hostBuilder.UseSerilog((context, serviceProvider, configuration) =>
            {
                var foundInEnv = Enum.TryParse(Environment.GetEnvironmentVariable("SERILOG_MINIMUMLEVEL"), true, out LogEventLevel minimumEnvLevel);
                if (foundInEnv)
                {
                    Enum.TryParse(Environment.GetEnvironmentVariable("SERILOG__MINIMUMLEVEL"), true, out minimumEnvLevel);
                }

                var conf = configuration
                    .ReadFrom.Configuration(serviceProvider.GetRequiredService<IConfiguration>()) // this also allows to set config vars from ENV
                    .MinimumLevel.Is(foundInEnv ? minimumEnvLevel : minimumLevel)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning); // override request logging

                conf.Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .Enrich.WithThreadName()
                    .Enrich.WithSpan()
                    .Enrich.With(new RemovePropertiesEnricher())
                    .ReadFrom.Services(serviceProvider);

                if (IsContainerProcess())
                {
                    conf.WriteTo.File("./logs/log.txt", outputTemplate: NonContainerTemplate, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 1073741824,
                            formatProvider: CultureInfo.InvariantCulture)
                        .WriteTo.Console(gelfJsonFormat);
                }
                else
                {
                    conf.WriteTo.Console(outputTemplate: NonContainerTemplate, formatProvider: CultureInfo.InvariantCulture);
                }
            }).ConfigureServices((context, services) => { services.AddSingleton<ILogger>(sp => sp.GetService<ILoggerFactory>().CreateLogger<ILogger>()); });
        }
    }
}
