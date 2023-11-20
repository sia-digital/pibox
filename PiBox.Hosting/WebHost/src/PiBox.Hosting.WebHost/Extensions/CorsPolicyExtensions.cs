using Microsoft.AspNetCore.Cors.Infrastructure;

namespace PiBox.Hosting.WebHost.Extensions
{
    public static class CorsPolicyExtensions
    {
        public static void SetSanityDefaults(this CorsPolicy corsPolicy)
        {
            if (!corsPolicy.Headers.Any())
                corsPolicy.Headers.Add("*");
            if (!corsPolicy.Methods.Any())
                corsPolicy.Methods.Add("*");
            if (!corsPolicy.Origins.Any())
                corsPolicy.Origins.Add("*");
        }
    }
}
