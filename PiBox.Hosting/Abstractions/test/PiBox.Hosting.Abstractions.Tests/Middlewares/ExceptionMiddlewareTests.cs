using System.Net;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Middlewares;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Hosting.Abstractions.Tests.Middlewares
{
    public class ExceptionMiddlewareTests
    {
        private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        private static HttpContext GetContext()
        {
            return new DefaultHttpContext
            {
                Response =
                {
                    Body = new MemoryStream()
                }
            };
        }

        private static T GetResponseContent<T>(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(context.Response.Body);
            var response = sr.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(response)!;
        }

        [SetUp]
        public void Setup()
        {
            _dateTimeProvider.UtcNow.Returns(new DateTime(2020, 1, 1));
        }

        [Test]
        public async Task MiddlewareHandlesUnauthorized()
        {
            var middleware = new ExceptionMiddleware(x =>
            {
                x.Response.StatusCode = 401;
                return Task.CompletedTask;
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions(), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(401);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("Unauthorized");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
        }

        [Test]
        public async Task MiddlewareHandlesForbidden()
        {
            var middleware = new ExceptionMiddleware(x =>
            {
                x.Response.StatusCode = 403;
                return Task.CompletedTask;
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions().Clear().Add(HttpStatusCode.Forbidden), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(403);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("Forbidden");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
        }

        [Test]
        public async Task MiddlewareHandlesNone()
        {
            var middleware = new ExceptionMiddleware(x =>
            {
                x.Response.StatusCode = 403;
                return Task.CompletedTask;
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions().Clear(), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(403);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().BeNull();
        }

        [Test]
        public async Task MiddlewareHandlesTeaPot()
        {
            var middleware = new ExceptionMiddleware(x =>
            {
                x.Response.StatusCode = StatusCodes.Status418ImATeapot;
                return Task.CompletedTask;
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions().Remove(401).Remove(HttpStatusCode.InternalServerError).Clear().Add(418), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(418);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("I'm a teapot");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
        }

        [Test]
        public async Task MiddlewareHandlesValidationPiBoxException()
        {
            var middleware = new ExceptionMiddleware(
                x => throw new ValidationPiBoxException("failure", new List<FieldValidationError> { new("x", "1 > x") }),
                NullLogger<ExceptionMiddleware>.Instance,
                new GlobalStatusCodeOptions().Clear(),
                _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(400);
            var errorResponse = GetResponseContent<ValidationErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("failure");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
            errorResponse.ValidationErrors.Should().HaveCount(1);
            var validationError = errorResponse.ValidationErrors.Single();
            validationError.ValidationMessage.Should().Be("1 > x");
            validationError.Field.Should().Be("x");
        }

        [Test]
        public async Task MiddlewareHandlesPiBoxException()
        {
            var middleware = new ExceptionMiddleware(
                x => throw new PiBoxException("test", 408),
                NullLogger<ExceptionMiddleware>.Instance,
                new GlobalStatusCodeOptions().Clear(),
                _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(408);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("test");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
        }

        [Test]
        public async Task MiddlewareWritesInternalServerErrorOnExceptions()
        {
            var middleware = new ExceptionMiddleware(_ => throw new Exception("test"), NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions(), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(500);
            var errorResponse = GetResponseContent<ErrorResponse>(context);
            errorResponse.Should().NotBeNull();
            errorResponse.Message.Should().Be("Internal Server Error");
            errorResponse.RequestId.Should().Be(context.TraceIdentifier);
            errorResponse.Timestamp.Should().Be(_dateTimeProvider.UtcNow);
        }

        [Test]
        public async Task MiddlewareSuccess()
        {
            var middleware = new ExceptionMiddleware(x =>
            {
                x.Response.StatusCode = 200;
                return Task.CompletedTask;
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions(), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            context.Response.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task MiddlewareDoesNothingWhenResponseStarted()
        {
            var responseFeature = Substitute.For<IHttpResponseFeature>();
            responseFeature.StatusCode = 101;
            responseFeature.HasStarted.Returns(true);
            var middleware = new ExceptionMiddleware(_ =>
            {
                _.Features.Set(responseFeature);
                throw new Exception("test");
            }, NullLogger<ExceptionMiddleware>.Instance, new GlobalStatusCodeOptions(), _dateTimeProvider);
            var context = GetContext();
            await middleware.Invoke(context);
            responseFeature.StatusCode.Should().Be(101);
        }
    }
}
