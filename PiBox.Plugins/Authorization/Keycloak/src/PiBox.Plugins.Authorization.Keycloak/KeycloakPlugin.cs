using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Plugins.Authorization.Abstractions;
using PiBox.Plugins.Authorization.Keycloak.Scheme;

namespace PiBox.Plugins.Authorization.Keycloak
{
    public class KeycloakPlugin : IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginHealthChecksConfiguration
    {
        private readonly KeycloakPluginConfiguration _keycloakPluginConfiguration;
        private readonly ILogger _logger;

        public KeycloakPlugin(KeycloakPluginConfiguration keycloakPluginKeycloakPluginConfiguration, ILogger<KeycloakPlugin> logger)
        {
            _logger = logger;
            _keycloakPluginConfiguration = keycloakPluginKeycloakPluginConfiguration;
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_keycloakPluginConfiguration);
            if (!_keycloakPluginConfiguration.Enabled)
            {
                _logger.LogWarning("Keycloak is disabled... No authentication is required!");
                serviceCollection.AddAuthentication();
                serviceCollection.AddAuthorization(o => o.AddDefaultPolicy(PolicyExtensions.AllowAllPolicy));
                return;
            }

            var authority = _keycloakPluginConfiguration.GetAuthority();
            serviceCollection.AddMemoryCache(o => o.ExpirationScanFrequency = TimeSpan.FromMinutes(-3));
            serviceCollection.AddHttpClient(nameof(PublicKeyService), o => o.BaseAddress = authority);
            serviceCollection.AddSingleton<IPublicKeyService, PublicKeyService>();

            serviceCollection
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, KeycloakAuthenticationHandler>(KeycloakDefaults.Scheme, null);

            serviceCollection.AddAuthorization(c =>
            {
                c.AddDefaultPolicy(new AuthorizationPolicyBuilder(KeycloakDefaults.Scheme).RequireAuthenticatedUser().Build());
                c.AddAuthPolicies(_keycloakPluginConfiguration.Policies);
            });
        }

        public void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseCookiePolicy(new() { Secure = CookieSecurePolicy.Always });
            applicationBuilder.UseAuthentication();
            applicationBuilder.UseAuthorization();
        }

        public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            var uriBuilder = new UriBuilder(_keycloakPluginConfiguration.GetHealthCheck()) { Path = _keycloakPluginConfiguration.HealthCheck.Prefix };
            var uri = uriBuilder.Uri;
            healthChecksBuilder.AddUrlGroup(uri, "keycloak", HealthStatus.Unhealthy, new[] { HealthCheckTag.Readiness.Value });
        }
    }
}
