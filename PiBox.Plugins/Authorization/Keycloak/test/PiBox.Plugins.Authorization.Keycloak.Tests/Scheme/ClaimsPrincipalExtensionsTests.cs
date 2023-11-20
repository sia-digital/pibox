using System.Security.Claims;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using PiBox.Plugins.Authorization.Keycloak.Scheme;

namespace PiBox.Plugins.Authorization.Keycloak.Tests.Scheme
{
    public class ClaimsPrincipalExtensionsTests
    {
        private static ClaimsPrincipal GetPrincipal(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        [Test]
        public void CanGetASpecificClaim()
        {
            var principal = GetPrincipal(new Claim("test", ""), new Claim("test", "123"));
            var valueOfClaim = principal.GetClaim("test");
            valueOfClaim.Should().NotBeNull();
            valueOfClaim.Should().Be("123");
        }

        [Test]
        public void CanGetEmptyRolesFromPrincipal()
        {
            var principal = GetPrincipal(new Claim(KeycloakDefaults.ClientClaim, "client"), new Claim(KeycloakDefaults.RealmAccessClaim, ""), new Claim(KeycloakDefaults.ResourceAccessClaim, ""));
            var roles = principal.GetRoleClaims().ToList();
            roles.Should().HaveCount(0);
        }

        [Test]
        public void CanGetEmptyRolesWhenTheRolesNodesAreMissing()
        {
            var realmAccess = JsonConvert.SerializeObject(new { });
            var resourceAccess = JsonConvert.SerializeObject(new { client = new { } });
            var principal = GetPrincipal(
                new Claim(KeycloakDefaults.ClientClaim, "client"),
                new Claim(KeycloakDefaults.RealmAccessClaim, realmAccess),
                new Claim(KeycloakDefaults.ResourceAccessClaim, resourceAccess));
            principal.GetRoleClaims().Should().HaveCount(0);
        }

        [Test]
        public void CanGetRoles()
        {
            var realmAccess = JsonConvert.SerializeObject(new { roles = new[] { "role1", "role2" } });
            var resourceAccess = JsonConvert.SerializeObject(new { client = new { roles = new[] { "role3", "role4" } } });
            var principal = GetPrincipal(
                new Claim(KeycloakDefaults.ClientClaim, "client"),
                new Claim(KeycloakDefaults.RealmAccessClaim, realmAccess),
                new Claim(KeycloakDefaults.ResourceAccessClaim, resourceAccess));
            var roles = principal.GetRoleClaims().Select(x => x.Value).ToList();
            roles.Should().HaveCount(4);
            roles.Should().Contain("role1");
            roles.Should().Contain("role2");
            roles.Should().Contain("role3");
            roles.Should().Contain("role4");
        }
    }
}
