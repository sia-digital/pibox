using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using PiBox.Extensions.RestEase.Authentication;
using Polly;
using RestEase;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PiBox.Extensions.RestEase.Tests
{
    public sealed class RestClientFactoryTests
    {
        private const string AuthorizationHeader = @"Authorization";
        private const string TokenPath = @"/auth/token";

        private string _baseAddress;
        private WireMockServer _server;

        [SetUp]
        public void Load()
        {
            _server = WireMockServer.Start(8087);
            _baseAddress = _server.Url;
        }

        [TearDown]
        public void Unload()
        {
            _server?.Dispose();
        }

        [Test]
        public async Task CanCreateRestClientWithBaseAddress()
        {
            var name = "the name";
            var returnObj = new SomeResource { Id = Guid.NewGuid(), Name = name };
            var serverRequest = Request.Create().WithPath("/").UsingPost();
            _server.Given(serverRequest)
                .RespondWith(Response.Create().WithSuccess().WithBody(JsonConvert.SerializeObject(returnObj)));
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress);
            client.Should().NotBeNull();
            var result = await client.Create(new SomeResource { Name = name }, CancellationToken.None);
            result.Id.Should().Be(returnObj.Id);
            result.Name.Should().Be(returnObj.Name);
            var logEntry = _server.FindLogEntries(serverRequest).FirstOrDefault()!;
            logEntry.Should().NotBeNull();
            logEntry.RequestMessage.Headers.Should().NotContainKey(AuthorizationHeader);
        }

        [Test]
        public async Task CanCreateRestClientWithBaseAddressAndAuthenticationConfig()
        {
            var name = "the name";
            var token = "abc";

            _server.Given(Request.Create().WithPath(TokenPath).UsingPost()).RespondWith(Response.Create().WithSuccess().WithBodyAsJson(new OAuth2Model { TokenType = "Bearer", AccessToken = token }));
            var returnObj = new SomeResource { Id = Guid.NewGuid(), Name = name };
            var serverRequest = Request.Create().WithPath("/").UsingPost();
            _server.Given(serverRequest)
                .RespondWith(Response.Create().WithSuccess().WithHeader(AuthorizationHeader, "Bearer", token).WithBody(JsonConvert.SerializeObject(returnObj)));
            var authConfig = new AuthenticationConfig
            {
                Enabled = true,
                TokenUrlPath = TokenPath,
                BaseUrl = _baseAddress,
                ClientId = "clientId",
                ClientSecret = "ClientSecret",
                GrantType = GrantType.Code,
                Username = "test",
                Password = "tester"
            };
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress, authConfig);
            client.Should().NotBeNull();
            var result = await client.Create(new SomeResource { Name = name }, CancellationToken.None);
            result.Id.Should().Be(returnObj.Id);
            result.Name.Should().Be(returnObj.Name);
            var logEntry = _server.FindLogEntries(serverRequest).FirstOrDefault()!;
            logEntry.Should().NotBeNull();
            logEntry.RequestMessage.Headers.Should().ContainKey(AuthorizationHeader);
            var authHeader = logEntry.RequestMessage.Headers[AuthorizationHeader].Single();
            authHeader.Should().Be("Bearer " + token);
        }

        [Test]
        public async Task DefaultRetryPolicyReruns()
        {
            var id = Guid.NewGuid();
            var requestMatcher = Request.Create().WithPath($"/{id}").UsingDelete();
            _server.Given(requestMatcher)
                .InScenario("test")
                .WillSetStateTo(1)
                .RespondWith(Response.Create().WithStatusCode(500));
            _server.Given(requestMatcher)
                .InScenario("test")
                .WhenStateIs(1)
                .RespondWith(Response.Create().WithSuccess());
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress);
            await client.Delete(id, CancellationToken.None);
            var entries = _server.FindLogEntries(requestMatcher).ToList();
            entries.Should().HaveCount(2);
        }

        [Test]
        public void ExceptionsGetsThrownOnFailedRequests()
        {
            var id = Guid.NewGuid();
            var requestMatcher = Request.Create().WithPath($"/{id}").UsingPut();
            _server.Given(requestMatcher)
                .RespondWith(Response.Create().WithStatusCode(500));
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress);
            var apiException = Assert.ThrowsAsync<ApiException>(async () => await client.Update(id, new SomeResource(), CancellationToken.None))!;
            apiException.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var entries = _server.FindLogEntries(requestMatcher).ToList();
            entries.Should().HaveCount(3);
        }

        [Test]
        public async Task CustomPolicyCanBeApplied()
        {
            var id = Guid.NewGuid();
            var requestMatcher = Request.Create().WithPath($"/{id}").UsingDelete();
            _server.Given(requestMatcher)
                .InScenario("test")
                .WillSetStateTo(1)
                .RespondWith(Response.Create().WithStatusCode(409));
            _server.Given(requestMatcher)
                .InScenario("test")
                .WhenStateIs(1)
                .RespondWith(Response.Create().WithSuccess());
            var policy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Conflict).RetryAsync(2);
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress, policy);
            await client.Delete(id, CancellationToken.None);
            var entries = _server.FindLogEntries(requestMatcher).ToList();
            entries.Should().HaveCount(2);
        }

        [Test]
        public async Task AuthHandlerRefreshesTheAccessToken()
        {
            var authConfig = new AuthenticationConfig
            {
                Enabled = true,
                TokenUrlPath = TokenPath,
                BaseUrl = _baseAddress,
                ClientId = "test",
                ClientSecret = "testing-secret",
                GrantType = GrantType.Password,
                Username = "test",
                Password = "tester"
            };
            var refreshToken = "refreshX";
            _server.Given(Request.Create()
                    .WithPath(TokenPath)
                    .WithBody((string content) => content.Contains("grant_type=password"))
                    .UsingPost())
                .RespondWith(Response.Create().WithSuccess().WithBodyAsJson(new OAuth2Model { TokenType = "Bearer", AccessToken = "123", ExpiresIn = 1, RefreshExpiresIn = 70, RefreshToken = refreshToken }));
            _server.Given(Request.Create()
                    .WithPath(TokenPath)
                    .WithBody((string content) => content.Contains("grant_type=refresh_token"))
                    .UsingPost())
                .RespondWith(Response.Create().WithSuccess().WithBodyAsJson(new OAuth2Model { TokenType = "Bearer", AccessToken = "new123", ExpiresIn = 60, Created = DateTime.UtcNow.AddSeconds(-61), RefreshExpiresIn = 70, RefreshToken = refreshToken }));
            var returnObj = new SomeResource { Id = Guid.NewGuid(), Name = "the name" };
            _server.Given(Request.Create()
                    .WithPath("/").WithHeader(AuthorizationHeader, "Bearer 123").UsingPost())
                .RespondWith(Response.Create().WithSuccess().WithBodyAsJson(new SomeResource()));
            _server.Given(Request.Create()
                    .WithPath("/").WithHeader(AuthorizationHeader, "Bearer new123").UsingPost())
                .RespondWith(Response.Create().WithSuccess().WithBodyAsJson(returnObj));
            var client = RestClientFactory.Create<ISomeApi>(_baseAddress, authConfig);
            await client.Create(new SomeResource(), CancellationToken.None);
            var secondCallResult = await client.Create(new SomeResource(), CancellationToken.None);
            secondCallResult.Id.Should().Be(returnObj.Id);
            secondCallResult.Name.Should().Be(returnObj.Name);
        }
    }
}
