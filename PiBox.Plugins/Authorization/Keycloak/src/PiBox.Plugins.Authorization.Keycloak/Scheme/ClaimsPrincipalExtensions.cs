using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace PiBox.Plugins.Authorization.Keycloak.Scheme
{
    internal static class ClaimsPrincipalExtensions
    {
        public static string GetClaim(this ClaimsPrincipal claimsPrincipal, string type) =>
            claimsPrincipal.Claims
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Value)
                    && x.Type == type)?.Value;

        public static IEnumerable<Claim> GetRoleClaims(this ClaimsPrincipal principal)
        {
            var roles = ParseRealmAccess(principal.GetClaim(KeycloakDefaults.RealmAccessClaim)).ToList();
            var authenticatedClientId = principal.GetClaim(KeycloakDefaults.ClientClaim);
            if (authenticatedClientId is not null)
                roles.AddRange(ParseResourceAccess(authenticatedClientId, principal.GetClaim(KeycloakDefaults.ResourceAccessClaim)));
            return roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();
        }

        private static IEnumerable<string> ParseResourceAccess(string clientId, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return Array.Empty<string>();
            var jObj = JObject.Parse(jsonContent);
            if (!jObj.ContainsKey(clientId)) return Array.Empty<string>();
            var roles = jObj[clientId]?["roles"]?.ToObject<string[]>();
            return ParseRoles(roles ?? Array.Empty<string>());
        }

        private static IEnumerable<string> ParseRealmAccess(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return Array.Empty<string>();
            var jObj = JObject.Parse(jsonContent);
            if (!jObj.ContainsKey("roles")) return Array.Empty<string>();
            var roles = jObj["roles"]!.ToObject<string[]>();
            return ParseRoles(roles ?? Array.Empty<string>());
        }

        private static IEnumerable<string> ParseRoles(IEnumerable<string> roles) =>
            roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();
    }
}
