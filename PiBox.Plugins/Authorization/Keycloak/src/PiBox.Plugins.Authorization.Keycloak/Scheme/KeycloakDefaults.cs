namespace PiBox.Plugins.Authorization.Keycloak.Scheme
{
    internal static class KeycloakDefaults
    {
        public const string Scheme = @"keycloak";
        public const string RealmAccessClaim = @"realm_access";
        public const string ResourceAccessClaim = @"resource_access";
        public const string ClientClaim = @"azp";

        // replace redirect uri scheme with https if not running on localhost
        // REASON: in other environments it seems the incoming request scheme is not the real one because of proxies/loadbalancer and
        // the respective x-forwarded-for headers are missing or not evaluated correctly
        internal static string BuildCorrectRedirectUri(string uri)
        {
            var builder = new UriBuilder(uri);
            if (builder.Uri.IsLoopback)
                return builder.ToString();
            builder.Scheme = Uri.UriSchemeHttps;
            builder.Port = -1;

            return builder.ToString();
        }
    }
}
