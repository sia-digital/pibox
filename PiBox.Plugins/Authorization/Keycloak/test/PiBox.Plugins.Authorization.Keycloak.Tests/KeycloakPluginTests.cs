using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using PiBox.Hosting.Abstractions;
using PiBox.Plugins.Authorization.Keycloak.Scheme;
using PiBox.Testing.Extensions;

namespace PiBox.Plugins.Authorization.Keycloak.Tests
{
    public class KeycloakPluginTests
    {
        private static KeycloakPlugin GetPlugin(KeycloakPluginConfiguration configuration) => new KeycloakPlugin(configuration, NullLogger<KeycloakPlugin>.Instance);

        [Test]
        public async Task AuthenticationCanBeDisabled()
        {
            var config = new KeycloakPluginConfiguration { Enabled = false };
            var sc = new ServiceCollection();
            GetPlugin(config).ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();
            var authorizationOptions = sp.GetService<IOptions<AuthorizationOptions>>()?.Value;
            authorizationOptions.Should().NotBeNull();
            authorizationOptions!.DefaultPolicy.Should().NotBeNull();
            authorizationOptions.DefaultPolicy.Requirements.Should().HaveCount(1);
            authorizationOptions.DefaultPolicy.Requirements[0].Should().BeOfType<AssertionRequirement>();
            var assertion = authorizationOptions.DefaultPolicy.Requirements[0] as AssertionRequirement;
            var isAllowed = await assertion!.Handler.Invoke(null!);
            isAllowed.Should().BeTrue();
        }

        [Test]
        public void AuthenticationCanBeSetup()
        {
            var config = new KeycloakPluginConfiguration { Enabled = true, Host = "example.com", Insecure = false, Port = 8080 };
            var sc = new ServiceCollection();
            GetPlugin(config).ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();
            var authorizationOptions = sp.GetService<IOptions<AuthorizationOptions>>()?.Value;
            authorizationOptions.Should().NotBeNull();
            authorizationOptions!.DefaultPolicy.Should().NotBeNull();
            authorizationOptions.DefaultPolicy.Requirements.Should().HaveCount(1);
            authorizationOptions.DefaultPolicy.Requirements[0].Should().BeOfType<DenyAnonymousAuthorizationRequirement>();
            var memoryCacheOptions = sp.GetService<IOptions<MemoryCacheOptions>>()?.Value;
            memoryCacheOptions.Should().NotBeNull();
            memoryCacheOptions!.ExpirationScanFrequency.Should().Be(TimeSpan.FromMinutes(-3));
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(PublicKeyService));
            httpClient.Should().NotBeNull();
            httpClient.BaseAddress.Should().NotBeNull();
            httpClient.BaseAddress!.Scheme.Should().Be("https");
            httpClient.BaseAddress!.Host.Should().Be("example.com");
            httpClient.BaseAddress!.Port.Should().Be(8080);
        }

        [Test]
        public void ThrowsArgumentExceptionWhenHostIsEmpty()
        {
            var config = new KeycloakPluginConfiguration { Enabled = true };
            var sc = new ServiceCollection();
            var configureCall = () => GetPlugin(config).ConfigureServices(sc);
            configureCall.Should().Throw<ArgumentException>("Keycloak.Host was not specified but authentication is enabled!");
        }

        [Test]
        public void ConfigureApplicationWorks()
        {
            var appBuilder = Substitute.For<IApplicationBuilder>();
            var sc = new ServiceCollection();
            sc.AddAuthentication();
            sc.AddAuthorization();
            var sp = sc.BuildServiceProvider();
            appBuilder.ApplicationServices.Returns(sp);
            GetPlugin(new KeycloakPluginConfiguration()).ConfigureApplication(appBuilder);
            var useCalls = (
                from call in appBuilder.ReceivedCalls()
                where call.GetMethodInfo().Name == "Use"
                select call).ToList();
            AssertMiddleware<CookiePolicyMiddleware>(useCalls[0]);
            AssertMiddleware<AuthenticationMiddleware>(useCalls[1]);
            AssertMiddleware<AuthorizationMiddleware>(useCalls[2]);
        }

        [Test]
        public void ConfigureHealthChecksWorks()
        {
            var hcBuilder = Substitute.For<IHealthChecksBuilder>();
            hcBuilder.Services.Returns(new ServiceCollection());
            var config = new KeycloakPluginConfiguration { Enabled = true, Host = "example.com", Insecure = false, HealthCheck = new HealthCheckConfig { Host = "example.com" } };
            GetPlugin(config).ConfigureHealthChecks(hcBuilder);
            hcBuilder.Received()
                .Add(Arg.Is<HealthCheckRegistration>(h =>
                    h.Name == "keycloak" && h.Tags.Contains(HealthCheckTag.Readiness.Value)));
        }

        [TestCase("http://localhost:5300/signin-oidc", "http://localhost:5300/signin-oidc")]
        public void TestRedirectUriHttpToHttpsReplace(string uri, string expected)
        {
            KeycloakDefaults.BuildCorrectRedirectUri(uri).Should().Be(expected);
        }

        [Test]
        public void ConfigureHealthChecks_Use9000ForHealth()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "example.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Host = "example.com",
                    Port = 9000,
                    Prefix = "/health/ready"
                }
            };
            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("http://example.com:9000/health/ready"));
        }

        [Test]
        public void ConfigureHealthChecks_UseInsecureAsDefaultForHealth()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "example.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Host = "example.com",
                    Port = 9000,
                    Prefix = "/health/ready"
                }
            };
            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("http://example.com:9000/health/ready"));
        }

        [Test]
        public void ConfigureHealthChecks_InsecureFalseForcesHttps()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "example.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Host = "example.com",
                    Port = 9000,
                    Prefix = "/health/ready",
                    Insecure = false
                }
            };
            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("https://example.com:9000/health/ready"));
        }

        [Test]
        public void ConfigureHealthChecks_WithSettingHealthCheckHost()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "example.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Host = "example.com"
                }
            };

            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("http://example.com:9000/health/ready"));
        }

        [Test]
        public void ConfigureHealthChecks_DifferentPrefixAndPort()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "newhost.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Host = "health.com",
                    Port = 9999,
                    Prefix = "/something/notready"
                }
            };
            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("http://health.com:9999/something/notready"));
        }

        [Test]
        public void ConfigureHealthChecks_DefaultHealthHost()
        {
            var config = new KeycloakPluginConfiguration
            {
                Enabled = true,
                Host = "newhost.com",
                Insecure = false,
                Port = 8080,
                HealthCheck = new HealthCheckConfig
                {
                    Port = 9999,
                    Prefix = "/something/notready",
                    Host = "example.com"
                }
            };
            var uriBuilder = new UriBuilder(config.GetHealthCheck()) { Path = config.HealthCheck.Prefix };
            uriBuilder.Uri.Should().Be(new Uri("http://example.com:9999/something/notready"));
        }

        private static void AssertMiddleware<TMiddleware>(ICall call)
        {
            var func = (call.GetOriginalArguments()[0] as Func<RequestDelegate, RequestDelegate>)?.Target;
            func.Should().NotBeNull();
            var registeredMiddleware = func!.GetInaccessibleValue<Type>("_middleware");
            if (registeredMiddleware.BaseType == typeof(TMiddleware))
                return;
            registeredMiddleware.Should().Be(typeof(TMiddleware));
        }
    }
}
