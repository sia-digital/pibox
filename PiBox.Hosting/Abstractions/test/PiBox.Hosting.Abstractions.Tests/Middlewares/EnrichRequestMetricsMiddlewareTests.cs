using System.Security.Claims;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Middlewares;

namespace PiBox.Hosting.Abstractions.Tests.Middlewares
{
    public class EnrichRequestMetricsMiddlewareTests
    {
        private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        private static HttpContext GetContext()
        {
            return new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        }

        [SetUp]
        public void Setup()
        {
            _dateTimeProvider.UtcNow.Returns(new DateTime(2020, 1, 1));
        }

        [Test]
        public async Task ExistingAzpClaimShouldAddTagWithUserIdToMetrics()
        {
            var httpMetricsTagsFeature = Substitute.For<IHttpMetricsTagsFeature>();
            httpMetricsTagsFeature.Tags.Returns(new List<KeyValuePair<string, object>>());
            var middleware = new EnrichRequestMetricsMiddleware(x =>
            {
                x.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, _dateTimeProvider);
            var context = GetContext();

            context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new Claim("azp", "userid1") }));
            context.Features.Set(httpMetricsTagsFeature);

            await middleware.Invoke(context);
            var metrics = context.Features.Get<IHttpMetricsTagsFeature>();
            metrics.Tags.Should().Contain(x => x.Key == "azp" && x.Value.ToString() == "userid1");
        }

        [Test]
        public async Task NonExistingAzpClaimShouldAddTagWithEmptyValueToMetrics()
        {
            var httpMetricsTagsFeature = Substitute.For<IHttpMetricsTagsFeature>();
            httpMetricsTagsFeature.Tags.Returns(new List<KeyValuePair<string, object>>());
            var middleware = new EnrichRequestMetricsMiddleware(x =>
            {
                x.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, _dateTimeProvider);
            var context = GetContext();

            context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new Claim("azp1", "userid1") }));
            context.Features.Set(httpMetricsTagsFeature);

            await middleware.Invoke(context);
            var metrics = context.Features.Get<IHttpMetricsTagsFeature>();
            metrics.Tags.Should().Contain(x => x.Key == "azp" && x.Value.ToString() == "");
        }

        [Test]
        public async Task NoHttpMetricsTagsFeatureShouldNotAddMetricTagsToResponse()
        {
            var httpMetricsTagsFeature = Substitute.For<IHttpMetricsTagsFeature>();
            httpMetricsTagsFeature.Tags.Returns(new List<KeyValuePair<string, object>>());
            var middleware = new EnrichRequestMetricsMiddleware(x =>
            {
                x.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, _dateTimeProvider);
            var context = GetContext();

            context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>() { new Claim("azp1", "userid1") }));

            await middleware.Invoke(context);
            var metrics = context.Features.Get<IHttpMetricsTagsFeature>();
            metrics.Should().BeNull();
        }
    }
}
