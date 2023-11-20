using System.Net;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Middlewares;

namespace PiBox.Hosting.Abstractions.Tests.Middlewares
{
    public class RequestContentLengthLimitMiddlewareTests
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
        public async Task MiddlewareFailure()
        {
            var requestMaxBodySizeFeature = Substitute.For<IHttpMaxRequestBodySizeFeature>();
            requestMaxBodySizeFeature.MaxRequestBodySize.Returns(8388608);
            var middleware = new RequestContentLengthLimitMiddleware(_ => Task.CompletedTask,
                NullLogger<RequestContentLengthLimitMiddleware>.Instance, _dateTimeProvider);
            var context = GetContext();
            context.Request.Method = WebRequestMethods.Http.Post;
            context.Request.ContentLength = 8388609;
            context.Features.Set(requestMaxBodySizeFeature);

            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(413);
        }

        [Test]
        public async Task MiddlewareSuccess()
        {
            var requestMaxBodySizeFeature = Substitute.For<IHttpMaxRequestBodySizeFeature>();
            requestMaxBodySizeFeature.MaxRequestBodySize.Returns(8388610);
            var middleware = new RequestContentLengthLimitMiddleware(x =>
            {
                x.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, NullLogger<RequestContentLengthLimitMiddleware>.Instance, _dateTimeProvider);
            var context = GetContext();
            context.Request.Method = WebRequestMethods.Http.Post;
            context.Request.ContentLength = 8388609;
            context.Features.Set(requestMaxBodySizeFeature);

            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(200);
        }
    }
}
