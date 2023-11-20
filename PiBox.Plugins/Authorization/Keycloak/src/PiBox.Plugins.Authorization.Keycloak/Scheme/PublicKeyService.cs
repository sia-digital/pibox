using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace PiBox.Plugins.Authorization.Keycloak.Scheme
{
    internal sealed class PublicKeyService : IPublicKeyService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly KeycloakPluginConfiguration _keycloakPluginKeycloakPluginConfiguration;

        public PublicKeyService(IHttpClientFactory httpClientFactory, ILogger<PublicKeyService> logger, IMemoryCache cache, KeycloakPluginConfiguration keycloakPluginKeycloakPluginConfiguration)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(PublicKeyService));
            _logger = logger;
            _cache = cache;
            _keycloakPluginKeycloakPluginConfiguration = keycloakPluginKeycloakPluginConfiguration;
        }

        public async Task<RsaSecurityKey> GetSecurityKey(string realm, CancellationToken cancellationToken = default)
        {
            var existingSecurityKey = _cache.Get<RsaSecurityKey>(realm);
            if (existingSecurityKey is not null) return existingSecurityKey;
            var securityKey = await GetCurrentSecurityKey(realm, cancellationToken);
            if (securityKey is null) return null;
            _cache.Set(realm, securityKey);
            return securityKey;
        }

        private async Task<RsaSecurityKey> GetCurrentSecurityKey(string realm, CancellationToken cancellationToken)
        {
            try
            {
                var realmDetails = (await _httpClient.GetFromJsonAsync<RealmDetails>($"{_keycloakPluginKeycloakPluginConfiguration.Realms.Prefix.TrimEnd('/')}/{realm}", cancellationToken))!;
                return GetRsaSecurityKey(realmDetails);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not get public key for {Realm}", realm);
                return null;
            }
        }

        private static RsaSecurityKey GetRsaSecurityKey(RealmDetails realmDetails)
        {
            var publicKey = Convert.FromBase64String(realmDetails.PublicKey);
            var rsaKeyParameters = (RsaKeyParameters)PublicKeyFactory.CreateKey(publicKey);
            var rsaParameters = new RSAParameters
            {
                Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned(),
                Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned()
            };
            return new RsaSecurityKey(rsaParameters);
        }

        private sealed class RealmDetails
        {
            [JsonPropertyName("public_key")]
            public string PublicKey { get; set; } = null!;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
