using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using PiBox.Plugins.Authorization.Keycloak.Scheme;
using RichardSzalay.MockHttp;

namespace PiBox.Plugins.Authorization.Keycloak.Tests.Scheme
{
    [SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method")]
    public class PublicKeyServiceTests
    {
        private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();
        private const string BaseAddress = "https://example.com";
        private const string DefaultRealm = "test";
        private const string DefaultIssuer = $"{BaseAddress}/auth/realms/{DefaultRealm}";

        private static IHttpClientFactory GetFactory(HttpClient httpClient)
        {
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
            return factory;
        }

        [Test]
        public async Task RsaSecurityKeyCanBeServedFromCache()
        {
            var httpFactory = GetFactory(null!);
            var key = new RsaSecurityKey(RSA.Create());
            _cache.TryGetValue(DefaultRealm, out Arg.Any<object>())
                .Returns(x =>
                {
                    x[1] = key;
                    return true;
                });
            var service = new PublicKeyService(httpFactory, NullLogger<PublicKeyService>.Instance, _cache, new());
            var secKey = await service.GetSecurityKey(DefaultRealm);
            secKey.Should().Be(key);
        }

        [Test]
        public async Task RsaSecurityKeyCanBeGetFromIssuer()
        {
            const string publicKey =
                "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAmO3ZwHyJwLm0Fn5ZK0SvULO3gGGHeHNbn5naCDyh1QxkBQluVxzVqEhRMal6J5QpnN8mKmDVfD1id68rgtrAdOOT0kCa0TZxSe7H3UD5zF53wEXSgA4t0UFpiax7ALn1sbfLz6UIRUfy0WWcmd0ewE30SEfuzT7cbl+g68mPjBP59CQRSPZP9I0yrug8x/VFB5BUJ78CGx8s4IzDP7j3SIUuDI53nGWenYbpOZxyK3vSCA+uTT2Jyn0whwwufCg+Ajrfq77vq2/BTTQwINeZyQ1miSMIape0GD7Ybsj/mcULuMzsMx1NqpRJl0QZLNI1fA87UjxovOyGmsJY6RPxHQIDAQAB";
            var httpClientMock = new MockHttpMessageHandler();
            httpClientMock.When(HttpMethod.Get, DefaultIssuer)
                .Respond("application/json", JsonConvert.SerializeObject(new { public_key = publicKey }));
            var httpClient = httpClientMock.ToHttpClient();
            httpClient.BaseAddress = new Uri(BaseAddress);
            var httpFactory = GetFactory(httpClient);
            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);
            var service = new PublicKeyService(httpFactory, NullLogger<PublicKeyService>.Instance, _cache, new());
            var secKey = await service.GetSecurityKey(DefaultRealm);

            secKey.Should().NotBeNull();
            _cache.CreateEntry(DefaultRealm).Received(Quantity.Exactly(1));
        }

        [Test]
        public async Task ReturnsNullAsKeyWhenSomethingBadHappens()
        {
            const string issuer = "https://example.com/auth";
            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);
            var httpClientMock = new MockHttpMessageHandler();
            httpClientMock.When(HttpMethod.Get, issuer)
                .Respond(HttpStatusCode.NotFound);
            using (var httpClient = httpClientMock.ToHttpClient())
            {
                var factory = GetFactory(httpClient);
                var service = new PublicKeyService(factory, NullLogger<PublicKeyService>.Instance, _cache, new());
                var secKey = await service.GetSecurityKey(issuer);
                secKey.Should().BeNull();
            }
            using (var httpClient = httpClientMock.ToHttpClient())
            {
                httpClient.BaseAddress = new Uri(issuer);
                var factory = GetFactory(httpClient);
                var service = new PublicKeyService(factory, NullLogger<PublicKeyService>.Instance, _cache, new());
                var secKey = await service.GetSecurityKey(issuer);
                secKey.Should().BeNull();
            }
        }

        [Test]
        public void HttpClientWillBeDisposed()
        {
            var httpClient = Substitute.For<HttpClient>();
            var factory = GetFactory(httpClient);
            using (var _ = new PublicKeyService(factory, NullLogger<PublicKeyService>.Instance, _cache, new()))
            {
                _.Should().NotBeNull(); // only for not having a warning
            }

            httpClient.ReceivedCalls().Any(x => x.GetMethodInfo().Name == "Dispose").Should().BeTrue();
        }
    }
}
