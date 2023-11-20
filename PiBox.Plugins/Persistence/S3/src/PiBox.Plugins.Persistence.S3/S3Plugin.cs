using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.S3
{
    public class S3Plugin : IPluginServiceConfiguration, IPluginHealthChecksConfiguration
    {
        private readonly S3Configuration _configuration;

        public S3Plugin(S3Configuration configuration)
        {
            _configuration = configuration;
        }

        private MinioClient GetMinioClient()
        {
            var minioClient = new MinioClient().WithEndpoint(_configuration.Endpoint);
            if (!string.IsNullOrEmpty(_configuration.AccessKey))
                minioClient = minioClient.WithCredentials(_configuration.AccessKey, _configuration.SecretKey);
            if (!string.IsNullOrEmpty(_configuration.Region))
                minioClient = minioClient.WithRegion(_configuration.Region);
            if (_configuration.UseSsl)
                minioClient = minioClient.WithSSL();
            return minioClient.Build();
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(GetMinioClient());
            serviceCollection.AddSingleton<IObjectOperations>(sp => sp.GetRequiredService<MinioClient>());
            serviceCollection.AddSingleton<IBlobStorage, S3BlobStorage>();
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            var urlScheme = _configuration.UseSsl ? "https" : "http";
            var url = $"{urlScheme}://{_configuration.Endpoint}";
            healthChecksBuilder.AddUrlGroup(new Uri(url), "s3", HealthStatus.Unhealthy, tags: new[] { HealthCheckTag.Readiness.Value });
        }
    }
}
