using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using FluentAssertions;
using Jose;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Authorization.Keycloak.Scheme;
using PiBox.Testing;

namespace PiBox.Plugins.Authorization.Keycloak.Tests.Scheme
{
    public class KeycloakAuthenticationHandlerTests
    {
        private readonly IPublicKeyService _publicKeyService = Substitute.For<IPublicKeyService>();
        private readonly UrlEncoder _urlEncoder = new UrlTestEncoder();

        private readonly IOptionsMonitor<AuthenticationSchemeOptions> _authSchemeOptions =
            Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();

        private readonly RSA _privateKey = RSA.Create();
        private const string DefaultRealm = "test";
        private const string DefaultClient = "unit-test";
        private const string DefaultUser = "test.user";
        private const string DefaultIssuer = $"https://example.com/auth/realms/{DefaultRealm}";

        public KeycloakAuthenticationHandlerTests()
        {
            ActivityTestBootstrapper.Setup();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _privateKey.Dispose();
        }

        private async Task<KeycloakAuthenticationHandler> GetHandler(HttpContext context)
        {
            _authSchemeOptions.Get(KeycloakDefaults.Scheme).Returns(new AuthenticationSchemeOptions());
            var handler = new KeycloakAuthenticationHandler(_publicKeyService, _authSchemeOptions,
                NullLogger<KeycloakAuthenticationHandler>.Instance, _urlEncoder);
            await handler.InitializeAsync(
                new AuthenticationScheme(KeycloakDefaults.Scheme, KeycloakDefaults.Scheme,
                    typeof(KeycloakAuthenticationHandler)), context);
            return handler;
        }

        private static HttpContext CreateContext(string authHeader)
        {
            var context = new DefaultHttpContext
            {
                RequestAborted = new CancellationToken(),
                Response = { Body = new MemoryStream() }
            };
            if (authHeader is not null)
                context.Request.Headers.Authorization = authHeader;
            return context;
        }

        private string CreateBearerToken(
            string issuer = null, string clientId = null,
            DateTime? authTime = null, DateTime? expiredTime = null,
            string[] roles = null, Dictionary<string, IEnumerable<string>> clientRoles = null,
            params Claim[] claims)
        {
            var rsaParameters = _privateKey.ExportParameters(true);
            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaParameters);
            var payload = claims.ToDictionary(x => x.Type, x => (object)x.Value);
            payload.Add("iss", issuer ?? DefaultIssuer);
            payload.Add("iat", DateToUnix(authTime ?? DateTime.UtcNow));
            payload.Add("auth_time", DateToUnix(authTime ?? DateTime.UtcNow));
            payload.Add("exp", DateToUnix(expiredTime ?? DateTime.UtcNow.AddMinutes(5)));
            payload.Add("azp", clientId ?? DefaultClient);
            payload.Add("preferred_username", DefaultUser);
            payload.Add("realm_access", JsonConvert.SerializeObject(roles is not null ? new { roles } : new { }));
            if (clientRoles is null)
                payload.Add("resource_access", JsonConvert.SerializeObject(new { }));
            else
            {
                var clientRolesPayload = clientRoles
                    .Select(x => new KeyValuePair<string, object>(x.Key, new { roles = x.Value }))
                    .ToDictionary(x => x.Key, x => x.Value);
                payload.Add("resource_access", JsonConvert.SerializeObject(clientRolesPayload));
            }

            _publicKeyService.GetSecurityKey(DefaultRealm, Arg.Any<CancellationToken>())
                .Returns(new RsaSecurityKey(_privateKey));
            return "Bearer " + JWT.Encode(payload, rsa, JwsAlgorithm.RS256);
        }

        private static string DateToUnix(DateTime dateTime) =>
            ((DateTimeOffset)dateTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        [Test]
        public async Task WillHandleNoTokenProvided()
        {
            using var metricsCollector = new TestMetricsCollector("authentication_keycloak_noresult");
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            var context = CreateContext(null);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeFalse();

            metricsCollector.Instruments.Should().Contain("authentication_keycloak_noresult");
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public async Task WillSucceedWithAValidToken()
        {
            using var metricsCollector = new TestMetricsCollector("authentication_keycloak_success");
            metricsCollector.CollectedMetrics.Should().BeEmpty();

            var token = CreateBearerToken();
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.GetClaim("preferred_username").Should().Be("test.user");
            result.Properties!.ExpiresUtc.Should().NotBeNull();

            metricsCollector.Instruments.Should().Contain("authentication_keycloak_success");
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
        }

        [Test]
        public async Task WillNotSucceedWhenTokenLifetimeIsTooOld()
        {
            var token = CreateBearerToken(expiredTime: DateTime.UtcNow.AddMinutes(-30));
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task WillNotSucceedWhenIssuerDoesNotMatch()
        {
            var token = CreateBearerToken();
            var context = CreateContext(token);
            _publicKeyService.GetSecurityKey("test").Returns((RsaSecurityKey)null);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task WillNotSucceedWhenTheIssuerHasNoRealmPart()
        {
            var token = CreateBearerToken(issuer: "https://example.com/auth");
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task RolesWillBeAddedToThePrincipal()
        {
            var token = CreateBearerToken(roles: new[] { "test", "test2", "" });
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.Claims.Should().NotBeNull();
            var roles = result.Principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            roles.Should().HaveCount(2);
            roles.Should().Contain("test");
            roles.Should().Contain("test2");
        }

        [Test]
        public async Task ClientRolesWillBeAddedToThePrincipal()
        {
            var clientRoles =
                new Dictionary<string, IEnumerable<string>> { { DefaultClient, new[] { "test", "test2", "" } } };
            var token = CreateBearerToken(clientRoles: clientRoles);
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.Claims.Should().NotBeNull();
            var roles = result.Principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            roles.Should().HaveCount(2);
            roles.Should().Contain("test");
            roles.Should().Contain("test2");
        }

        [Test]
        public async Task ClientRolesAndRolesWillBeAddedToThePrincipal()
        {
            var clientRoles =
                new Dictionary<string, IEnumerable<string>> { { DefaultClient, new[] { "test", "test2", "" } } };
            var token = CreateBearerToken(roles: new[] { "role" }, clientRoles: clientRoles);
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.Claims.Should().NotBeNull();
            var roles = result.Principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            roles.Should().HaveCount(3);
            roles.Should().Contain("test");
            roles.Should().Contain("test2");
            roles.Should().Contain("role");
        }

        [Test]
        public async Task ClientRolesWillOnlyBeAddedIfTheClientMatches()
        {
            var clientRoles = new Dictionary<string, IEnumerable<string>> { { "another-client", new[] { "test", "test2" } } };
            var token = CreateBearerToken(clientRoles: clientRoles);
            var context = CreateContext(token);
            var handler = await GetHandler(context);
            var result = await handler.AuthenticateAsync();
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.Claims.Should().NotBeNull();
            var roles = result.Principal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            roles.Should().HaveCount(0);
        }
    }
}
