using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using PiBox.Hosting.Abstractions.Metrics;

namespace PiBox.Plugins.Authorization.Keycloak.Scheme
{
    internal sealed class KeycloakAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ActivitySource _activitySource = new(nameof(KeycloakAuthenticationHandler));
        private readonly IPublicKeyService _publicKeyService;
        private readonly ILogger _logger;

        private readonly Counter<long> _successfulAuthenticationAttempts =
            Metrics.CreateCounter<long>("authentication_keycloak_success", "calls", "total count of authentication attempts with successful result");
        private readonly Counter<long> _noResultAuthenticationAttempts =
            Metrics.CreateCounter<long>("authentication_keycloak_noresult", "calls", "total count of authentication attempts with no result");
        private readonly JwtSecurityTokenHandler _securityTokenHandler = new();

        public KeycloakAuthenticationHandler(
            IPublicKeyService publicKeyService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILogger<KeycloakAuthenticationHandler> logger,
            UrlEncoder encoder
        ) :
            base(options, NullLoggerFactory.Instance, encoder)
        {
            _logger = logger;
            _publicKeyService = publicKeyService;
        }

        private AuthenticateResult HandleResult(string result, Activity activity)
        {
            activity.SetStatus(ActivityStatusCode.Error, result);
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            _logger.LogDebug("Failure: {Message}", result);
            _noResultAuthenticationAttempts.Add(1);
            return AuthenticateResult.NoResult();
        }
        private AuthenticateResult HandleResult(AuthenticationTicket result, Activity activity)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            _successfulAuthenticationAttempts.Add(1);
            return AuthenticateResult.Success(result);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            using var activity = _activitySource.StartActivity(nameof(HandleAuthenticateAsync), kind: ActivityKind.Internal, parentContext: default);
            var token = Request.Headers[HeaderNames.Authorization].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
                return HandleResult("Could not find header 'authorization'", activity!);
            if (!token.StartsWith("Bearer", StringComparison.InvariantCultureIgnoreCase))
                return HandleResult("Token is not of type bearer", activity!);

            var jwtToken = GetJwtToken(token);
            var principal = await GetPrincipalByJwtToken(jwtToken, Context.RequestAborted);
            if (principal is null) return HandleResult("Could not read or validate principal.", activity!);

            var authTicket = new AuthenticationTicket(principal, SetAuthenticationProperties(jwtToken), KeycloakDefaults.Scheme);
            return HandleResult(authTicket, activity!);
        }

        private async Task<ClaimsPrincipal> GetPrincipalByJwtToken(JwtSecurityToken token,
            CancellationToken cancellationToken)
        {
            var rsaKey = await GetRsaKey(token, cancellationToken);
            if (rsaKey is null) return null;
            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = rsaKey,
                ValidateAudience = false,
                ValidIssuer = token.Issuer,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
            try
            {
                var principal = _securityTokenHandler.ValidateToken(token.RawData, tokenValidationParameters, out _);
                if (principal?.Identity is ClaimsIdentity identity)
                    identity.AddClaims(principal.GetRoleClaims());
                return principal?.Identity is not null
                    ? new ClaimsPrincipal(principal.Identity)
                    : principal;
            }
            catch (SecurityTokenException e)
            {
                _logger.LogWarning("Could not validate token. {Reason}", e.Message);
                return null;
            }
        }

        private async Task<RsaSecurityKey> GetRsaKey(SecurityToken jwtToken, CancellationToken cancellationToken)
        {
            var issuer = jwtToken.Issuer.TrimEnd('/');
            var issuerParts = issuer.Split("/realms/");
            if (issuerParts.Length != 2) return null;
            var realm = issuerParts[1];
            return await _publicKeyService.GetSecurityKey(realm, cancellationToken);
        }

        private JwtSecurityToken GetJwtToken(string token)
        {
            token = token?.Replace("Bearer ", "").Trim() ?? "";
            return _securityTokenHandler.ReadJwtToken(token);
        }

        private static AuthenticationProperties SetAuthenticationProperties(JwtSecurityToken token)
        {
            return new AuthenticationProperties { ExpiresUtc = token.ValidTo };
        }
    }
}
