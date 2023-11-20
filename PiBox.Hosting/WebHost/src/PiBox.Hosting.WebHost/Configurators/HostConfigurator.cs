using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Configuration;
using PiBox.Hosting.WebHost.Logging;

namespace PiBox.Hosting.WebHost.Configurators
{
    internal class HostConfigurator
    {
        private readonly WebApplicationBuilder _builder;
        private readonly ILogger _logger;

        public HostConfigurator(WebApplicationBuilder builder, ILogger logger)
        {
            _builder = builder;
            _logger = logger;
        }

        public void Configure()
        {
            ConfigureLogging();
            ConfigureWebHost();
        }

        internal static void ConfigureAppConfiguration(ConfigurationManager configurationManager)
        {
#pragma warning disable ASP0013

            configurationManager.Sources.Clear();

            foreach (var jsonConfig in FindAppSettingFiles(".json"))
                configurationManager.AddJsonFile(jsonConfig, optional: true, reloadOnChange: true);

            foreach (var yamlFile in FindAppSettingFiles(".yaml", ".yml"))
                configurationManager.AddYamlFile(yamlFile, optional: true, reloadOnChange: true);

            configurationManager.AddJsonFile("appsettings.json", true, true)
                .AddYamlFile("appsettings.yaml", true, true)
                .AddYamlFile("appsettings.yml", true, true)
                .AddEnvVariables();
#pragma warning restore ASP0013
        }

        private static IEnumerable<string> FindAppSettingFiles(params string[] extensions)
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory)
                .Select(x => x.Split(Path.DirectorySeparatorChar).Last())
                .Where(x => !string.IsNullOrEmpty(x) && x.StartsWith("appsettings", StringComparison.InvariantCulture))
                .Where(x => extensions.Any(e => x.EndsWith(e, StringComparison.InvariantCulture)))
                .Where(x => !extensions.Any(
                    e => string.Equals(x, "appsettings" + e, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return files;
        }

        private void ConfigureLogging()
        {
            _builder.Host.UseStructuredLogging();
        }

        private void ConfigureWebHost()
        {
            var config = _builder.Configuration;
            var urls = config.GetValue<string>("host:urls")
                       ?? config.GetValue<string>("aspnetcore:urls")
                       ?? "http://+:8080";
            var maxRequestSize = config.GetValue<long?>("host:maxRequestSize") ?? 8388608; // 8 MB default
            _logger.LogInformation("Hosting urls {HostingUrls}", urls.Replace(",", " , "));
            _builder.WebHost.UseKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = maxRequestSize;
                options.AddServerHeader = false;
            }).UseUrls(urls.Split(','));
        }
    }
}
