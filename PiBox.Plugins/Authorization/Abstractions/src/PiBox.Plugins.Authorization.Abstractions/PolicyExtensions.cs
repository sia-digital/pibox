using Microsoft.AspNetCore.Authorization;

namespace PiBox.Plugins.Authorization.Abstractions
{
    public static class PolicyExtensions
    {
        private const string DefaultPolicyName = @"DEFAULT";

        public static readonly AuthorizationPolicy AuthenticatedPolicy =
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        public static readonly AuthorizationPolicy AllowAllPolicy =
            new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        public static void AddDefaultPolicy(this AuthorizationOptions options, AuthorizationPolicy policy)
        {
            options.DefaultPolicy = policy;
            options.AddPolicy(DefaultPolicyName, policy);
        }

        public static void AddAuthPolicies(this AuthorizationOptions options, IEnumerable<AuthPolicy> authPolicies)
        {
            foreach (var authPolicy in authPolicies)
            {
                options.AddPolicy(authPolicy.Name!, builder =>
                {
                    foreach (var authPolicyRole in authPolicy.Roles!)
                    {
                        builder.RequireRole(authPolicyRole);
                    }
                });
            }
        }
    }
}
