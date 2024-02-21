using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Configuration;
using PiBox.Hosting.WebHost.Extensions;
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

            var settings = FindAppSettingFiles("json", "yaml", "yml").ToList();
            foreach (var file in settings.Where(x => !IsSecretsFile(x)))
                configurationManager.AddFile(file);

            foreach (var file in settings.Where(IsSecretsFile))
                configurationManager.AddFile(file);

            configurationManager.AddEnvVariables();
#pragma warning restore ASP0013
        }

        private static bool IsSecretsFile(string filename) => filename.EndsWith(".secrets.yaml") ||
                                                             filename.EndsWith(".secrets.yml") ||
                                                             filename.EndsWith(".secrets.json");

        private static IEnumerable<string> FindAppSettingFiles(params string[] extensions)
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory)
                .Select(x => x.Split(Path.DirectorySeparatorChar).Last())
                .Where(x => !string.IsNullOrEmpty(x) && x.StartsWith("appsettings.", StringComparison.InvariantCulture))
                .Where(x => extensions.Any(e => x.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(x => x.Split('.').Length)
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
