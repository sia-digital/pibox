using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            var roles = ParseRealmAccess(principal.GetClaim(KeycloakDefaults.RealmAccessClaim));
            var authenticatedClientId = principal.GetClaim(KeycloakDefaults.ClientClaim);

            if (authenticatedClientId is not null)
                roles = roles.Concat(
                    ParseResourceAccess(
                        authenticatedClientId,
                        principal.GetClaim(KeycloakDefaults.ResourceAccessClaim)));

            return roles.Select(x => new Claim(ClaimTypes.Role, x));
        }

        private static IEnumerable<string> ParseResourceAccess(string clientId, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return Array.Empty<string>();

            var jObj = JsonNode.Parse(jsonContent);

            if (jObj == null || jObj.GetValueKind() != JsonValueKind.Object)
                return Array.Empty<string>();

            if (!jObj.AsObject().TryGetPropertyValue(clientId, out var jsonClientId))
                return Array.Empty<string>();

            if (jsonClientId == null
                || !jsonClientId.AsObject().TryGetPropertyValue("roles", out var jsonRoles)
                || jsonRoles == null
                || jsonRoles.GetValueKind() != JsonValueKind.Array)
                return Array.Empty<string>();

            var roles = jsonRoles.AsArray()
                .Select(node => node.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : string.Empty);
            return ParseRoles(roles);
        }

        private static IEnumerable<string> ParseRealmAccess(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return Array.Empty<string>();

            var jObj = JsonNode.Parse(jsonContent);

            if (jObj == null || jObj.GetValueKind() != JsonValueKind.Object)
                return Array.Empty<string>();

            if (!jObj.AsObject().TryGetPropertyValue("roles", out var jsonRoles)
                || jsonRoles == null
                || jsonRoles.GetValueKind() != JsonValueKind.Array)
                return Array.Empty<string>();

            var roles = jsonRoles.AsArray()
                .Select(node => node.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : string.Empty);
            return ParseRoles(roles);
        }

        private static IEnumerable<string> ParseRoles(IEnumerable<string> roles) =>
            roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();
    }
}
