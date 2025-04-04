namespace PiBox.Plugins.Authorization.Keycloak.Scheme
{
    public static class KeycloakDefaults
    {
        public const string Scheme = @"keycloak";
        public const string RealmAccessClaim = @"realm_access";
        public const string ResourceAccessClaim = @"resource_access";
        public const string ClientClaim = @"azp";
        public const string ClaimTypeRealm = "realm_name";
    }
}
