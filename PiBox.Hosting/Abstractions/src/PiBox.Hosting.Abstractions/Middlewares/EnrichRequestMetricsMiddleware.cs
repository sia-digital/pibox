using Chronos.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Hosting.Abstractions.Middlewares
{
    [Middleware]
    public sealed class EnrichRequestMetricsMiddleware : ApiMiddleware
    {
        public EnrichRequestMetricsMiddleware(RequestDelegate next, IDateTimeProvider dateTimeProvider) : base(next,
            dateTimeProvider)
        {
        }

        public override Task Invoke(HttpContext context)
        {
            var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
            if (tagsFeature == null) return Next.Invoke(context);
            var authorizedParty = context.User.Claims.SingleOrDefault(x => x.Type == "azp")?.Value;
            tagsFeature.Tags.Add(new KeyValuePair<string, object>("azp", authorizedParty ?? ""));

            return Next.Invoke(context);
        }
    }
}
