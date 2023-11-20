using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace PiBox.Extensions.RestEase.Authentication
{
    internal class AuthHandler
    {
        private readonly IDictionary<GrantType, string> _grantTypeMapping = new Dictionary<GrantType, string>
        {
            {GrantType.Code, "code"},
            {GrantType.Password, "password"},
            {GrantType.ClientCredentials, "client_credentials"}
        };
        private readonly IAuthApi _api;
        private readonly AuthenticationConfig _authConfig;
        private OAuth2Model _oauth2;

        public AuthHandler(AuthenticationConfig authConfig)
        {
            _authConfig = authConfig;
            _api = RestClient.For<IAuthApi>(authConfig.BaseUrl);
        }

        public async Task HandleAuth(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            if (!_authConfig.Enabled || requestMessage.Headers.Authorization != null) return;
            var tokenModel = await GetOAuth2Model(cancellationToken);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(tokenModel.TokenType, tokenModel.AccessToken);
        }

        private async Task<OAuth2Model> GetOAuth2Model(CancellationToken cancellationToken)
        {
            if (_oauth2 is null || _oauth2.ShouldCreateToken())
                _oauth2 = await GetToken(cancellationToken);
            else if (_oauth2.ShouldRefreshToken())
                _oauth2 = await RefreshToken(cancellationToken);
            return _oauth2;
        }

        private Task<OAuth2Model> RefreshToken(CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>
            {
                {"grant_type", "refresh_token"},
                {"client_id", _authConfig.ClientId},
                {"refresh_token", _oauth2!.RefreshToken}
            };
            if (!string.IsNullOrEmpty(_authConfig.ClientSecret))
                data.Add("client_secret", _authConfig.ClientSecret);
            return _api.GetToken(_authConfig.TokenUrlPath, data, cancellationToken);
        }

        private Task<OAuth2Model> GetToken(CancellationToken cancellationToken)
        {
            var data = GetTokenRequestBody();
            return _api.GetToken(_authConfig.TokenUrlPath, data, cancellationToken);
        }

        private IDictionary<string, object> GetTokenRequestBody()
        {
            var data = new Dictionary<string, object>();
            if (_authConfig.ClientId is not null)
                data.Add("client_id", _authConfig.ClientId);
            if (_authConfig.ClientSecret is not null)
                data.Add("client_secret", _authConfig.ClientSecret);
            if (_authConfig.Username is not null)
                data.Add("username", _authConfig.Username);
            if (_authConfig.Password is not null)
                data.Add("password", _authConfig.Password);
            data.Add("grant_type", _grantTypeMapping[_authConfig.GrantType]);
            return data;
        }
    }
}
