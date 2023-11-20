using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;
using OpenTelemetry.Trace;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Hosting.WebHost.Configurators;
using PiBox.Hosting.WebHost.Logging;
using PiBox.Testing;
using PiBox.Testing.Assertions;
using Serilog.Core;

namespace PiBox.Hosting.WebHost.Tests
{
    public class ServiceConfigurationTests
    {
        [Test]
        public void ConfigureTracingShouldWorkIfHostIsConfigured()
        {
            var configuration = CustomConfiguration.Create().Add("tracing:host", "localhost:1111").Build();
            var serviceCollection = TestingDefaults.ServiceCollection();

            var serviceConfigurator = new ServiceConfigurator(new FakeLogger<ServiceConfigurator>(), configuration, serviceCollection, Substitute.For<IImplementationResolver>());
            serviceConfigurator.ConfigureTracing();

            serviceCollection.Should().Contain(x => x.ServiceType == typeof(ILogEventEnricher) && x.ImplementationType == typeof(OpentelemetryTraceEnricher));
            serviceCollection.Should().Contain(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType!.Name == "TelemetryHostedService");
            serviceCollection.Should().Contain(x => x.ServiceType == typeof(TracerProvider));
        }

        [Test]
        public void ConfigureTracingShouldNotWorkIfHostIsNotConfigured()
        {
            var configuration = CustomConfiguration.Create().Build();
            var serviceCollection = TestingDefaults.ServiceCollection();

            var serviceConfigurator = new ServiceConfigurator(new FakeLogger<ServiceConfigurator>(), configuration, serviceCollection, Substitute.For<IImplementationResolver>());
            serviceConfigurator.ConfigureTracing();

            serviceCollection.Should().NotContain(x => x.ServiceType == typeof(ILogEventEnricher) && x.ImplementationType == typeof(OpentelemetryTraceEnricher));
            serviceCollection.Should().NotContain(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType!.Name == "TelemetryHostedService");
            serviceCollection.Should().NotContain(x => x.ServiceType == typeof(TracerProvider));
        }
    }
}
