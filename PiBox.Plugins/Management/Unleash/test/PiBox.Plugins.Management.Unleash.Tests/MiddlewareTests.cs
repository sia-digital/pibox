using System.Security.Claims;
using System.Security.Principal;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using PiBox.Testing;
using Unleash;

namespace PiBox.Plugins.Management.Unleash.Tests
{
    [TestFixture]
    public class UnleashMiddlewareTests
    {
        private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        private readonly DateTimeOffset _dateTimeOffset = new(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(0));

        private static HttpContext GetContext()
        {
            return new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        }

        [SetUp]
        public void Setup()
        {
            _dateTimeProvider.UtcNow.Returns(_dateTimeOffset.UtcDateTime);
        }

        [Test]
        public async Task MiddlewareWithoutSessionAndUserPropagatesContextCorrectly()
        {
            var sc = TestingDefaults.ServiceCollection();
            var sp = sc.BuildServiceProvider();
            var unleashConfiguration = UnleashPluginTests.GetUnleashConfiguration;
            var middleware = new UnleashMiddlware(_ => Task.CompletedTask,
                _dateTimeProvider, unleashConfiguration, sp);
            var context = GetContext();

            await middleware.Invoke(context);
            var propagatedContext = context.Items[UnleashMiddlware.Unleashcontext] as UnleashContext;
            propagatedContext!.AppName.Should().Be(unleashConfiguration.AppName);
            propagatedContext!.Environment.Should().Be(unleashConfiguration.Environment);
            propagatedContext!.CurrentTime.Should().Be(_dateTimeOffset);
            propagatedContext!.Properties.Should().BeEmpty();
            propagatedContext!.SessionId.Should().BeNull();
            propagatedContext!.UserId.Should().BeNull();
        }

        [Test]
        public async Task MiddlewareWithSessionAndUserPropagatesContextCorrectly()
        {
            var sc = TestingDefaults.ServiceCollection();
            sc.AddTransient<ISessionStore, DummySessionStore>();
            var sp = sc.BuildServiceProvider();
            var unleashConfiguration = UnleashPluginTests.GetUnleashConfiguration;
            var middleware = new UnleashMiddlware(_ => Task.CompletedTask,
                _dateTimeProvider, unleashConfiguration, sp);
            var context = GetContext();
            var sessionId = Guid.NewGuid().ToString();
            context.Session = new DummySession(sessionId);

            context.User = new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("test-user-1")));

            await middleware.Invoke(context);
            var propagatedContext = context.Items[UnleashMiddlware.Unleashcontext] as UnleashContext;
            propagatedContext!.AppName.Should().Be(unleashConfiguration.AppName);
            propagatedContext!.Environment.Should().Be(unleashConfiguration.Environment);
            propagatedContext!.CurrentTime.Should().Be(_dateTimeOffset);
            propagatedContext!.Properties.Should().BeEmpty();
            propagatedContext!.SessionId.Should().Be(sessionId);
            propagatedContext!.UserId.Should().Be("test-user-1");
        }
    }

    internal class DummySessionStore : ISessionStore
    {
        public ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout,
            Func<bool> tryEstablishSession,
            bool isNewSessionKey)
        {
            return new DummySession("empty-session");
        }
    }

    internal class DummySession : ISession
    {
        public DummySession(string id)
        {
            this.Id = id;
        }

        public Task LoadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // not needed for testing
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // not needed for testing
            return Task.CompletedTask;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            // not needed for testing
            value = Array.Empty<byte>();
            return false;
        }

        public void Set(string key, byte[] value)
        {
            // not needed for testing
        }

        public void Remove(string key)
        {
            // not needed for testing
        }

        public void Clear()
        {
            // not needed for testing
        }

        public bool IsAvailable { get; }

        public string Id { get; set; }

        public IEnumerable<string> Keys { get; }
    }
}
