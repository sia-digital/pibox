using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Plugins.Authorization.Abstractions;

namespace PiBox.Plugins.Authorization.Keycloak
{
    [Configuration("keycloak")]
    public class KeycloakPluginConfiguration
    {
        public bool Enabled { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }
        public bool Insecure { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public RealmsConfig Realms { get; set; } = new RealmsConfig();
        public IList<AuthPolicy> Policies { get; set; } = new List<AuthPolicy>();
        public HealthCheckConfig HealthCheck { get; set; } = new HealthCheckConfig();

        public Uri GetHealthCheck()
        {
            if (string.IsNullOrEmpty(HealthCheck.Host)) throw new ArgumentException("Keycloak.Uri was not specified but health check is enabled!");
            var httpScheme = (HealthCheck.Insecure ? HttpScheme.Http : HttpScheme.Https).ToString();
            return Port.HasValue
                ? new UriBuilder(httpScheme, HealthCheck.Host, HealthCheck.Port.Value).Uri
                : new UriBuilder(httpScheme, HealthCheck.Host).Uri;
        }

        public Uri GetAuthority()
        {
            if (string.IsNullOrEmpty(Host)) throw new ArgumentException("Keycloak.Host was not specified but authentication is enabled!");
            var httpScheme = (Insecure ? HttpScheme.Http : HttpScheme.Https).ToString();
            return Port.HasValue
                ? new UriBuilder(httpScheme, Host, Port.Value).Uri
                : new UriBuilder(httpScheme, Host).Uri;
        }
    }
    public class RealmsConfig
    {
        public string Prefix { get; set; } = "/auth/realms";
        public string Default { get; set; } = "master";
    }

    public class HealthCheckConfig
    {
        public bool Insecure { get; set; } = true;
        public string Host { get; set; }
        public int? Port { get; set; } = 9000;
        public string Prefix { get; set; } = "/health/ready";
    }
}

