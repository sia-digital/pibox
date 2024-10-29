using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Hosting.WebHost.Services;
using PiBox.Testing;

namespace PiBox.Hosting.WebHost.Tests
{
    public class TypeImplementationResolverTests
    {
        private readonly Type[] _resolvedTypes = new Type[] { typeof(SampleType), typeof(WithoutCtor), typeof(UnitTestPluginConfig) };
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

        [Test]
        public void CanFindASpecificType()
        {
            var typeService = new TypeImplementationResolver(_configuration, _resolvedTypes, new Dictionary<Type, object>());
            var types = typeService.FindTypes(f => f.Implements<BaseClass>());
            types.Should().Contain(typeof(SampleType));
            types.Should().NotContain(typeof(BaseClass));
        }

        [Test]
        public void CanFindAssembliesForPlugins()
        {
            var typeService = new TypeImplementationResolver(_configuration, _resolvedTypes, new Dictionary<Type, object>());
            var assemblies = typeService.FindAssemblies();
            assemblies.Should().HaveCount(1);
            var assembly = assemblies.Single();
            assembly.Should().BeSameAs(typeof(TypeImplementationResolverTests).Assembly);
            assemblies = typeService.FindAssemblies(f => f.HasType(t => t.HasAttribute<ConfigurationAttribute>()));
            assemblies.Should().HaveCount(1);
            assembly = assemblies.Single();
            assembly.Should().BeSameAs(typeof(TypeImplementationResolverTests).Assembly);
        }

        [Test]
        public void InstancesWillBeReused()
        {
            var config = CustomConfiguration.Empty;
            var defaultArguments = new Dictionary<Type, object>();
            var typeService = new TypeImplementationResolver(config, _resolvedTypes, defaultArguments);
            SampleType.CreationCount.Should().Be(0);
            var sampleType = typeService.ResolveInstance(typeof(SampleType));
            SampleType.CreationCount.Should().Be(1);
            sampleType.Should().BeOfType<SampleType>();
            var sampleType2 = typeService.ResolveInstance(typeof(SampleType));
            SampleType.CreationCount.Should().Be(1);
            sampleType.Should().Be(sampleType2);

            var obj = sampleType as SampleType;
            obj.Should().NotBeNull();
            obj!.GetConfig().Should().Be(config);
            SampleType.DisposeCount.Should().Be(0);
            typeService.ClearInstances();
            SampleType.DisposeCount.Should().Be(1);
            typeService.ResolveInstance(typeof(SampleType));
            SampleType.CreationCount.Should().Be(2);
        }

        [Test]
        public void CanActivateWithEmptyCtor()
        {
            IImplementationResolver implementationResolver = new TypeImplementationResolver(_configuration, _resolvedTypes, new Dictionary<Type, object>());
            var instances = implementationResolver.FindAndResolve<WithoutCtor>();
            instances.Should().HaveCount(1);
            instances.Single().GetTest().Should().Be("TEST");
        }

        [Test]
        public void CanResolvePluginConfigurations()
        {
            var configName = "my-config-name";
            var config = CustomConfiguration.Create();
            config.Add("sampleConfig:name", configName);
            var resolver = new TypeImplementationResolver(config.Build(), _resolvedTypes, new Dictionary<Type, object>());
            var pluginConfig = resolver.ResolveInstance(typeof(UnitTestPluginConfig)) as UnitTestPluginConfig;
            pluginConfig.Should().NotBeNull();
            pluginConfig!.Name.Should().Be(configName);
        }

        [Test]
        public void CanResolveConfigurators()
        {
            var resolver = new TypeImplementationResolver(_configuration, _resolvedTypes, new Dictionary<Type, object>());
            var instance = resolver.ResolveInstance(typeof(Configurator)) as Configurator;
            instance.Should().NotBeNull();
            instance!.GetMessage().Should().Be("Hello World!");

            var plugin = resolver.ResolveInstance(typeof(ConfiguratorPlugin)) as ConfiguratorPlugin;
            plugin.Should().NotBeNull();
            plugin!.Message.Should().Be("Hello World!");
            plugin!.Message2.Should().Be("Hello World!");
        }

        internal class Configurator : IConfiguratorPluginConfigurator
        {
            public string GetMessage() => "Hello World!";
        }

        internal interface IConfiguratorPluginConfigurator : IPluginConfigurator
        {
            string GetMessage();
        }

        internal class ConfiguratorPlugin(IConfiguratorPluginConfigurator[] configurators, IList<IConfiguratorPluginConfigurator> configurators2) : IPluginActivateable
        {
            public string Message = string.Join(" ", configurators.Select(c => c.GetMessage()));
            public string Message2 = string.Join(" ", configurators2.Select(c => c.GetMessage()));
        }

        [Configuration("sampleConfig")]
        internal class UnitTestPluginConfig
        {
            public string Name { get; set; } = null!;
        }

        internal abstract class BaseClass
        {
        }
#pragma warning disable S3881
        internal class SampleType : BaseClass, IDisposable
        {
            public static int CreationCount;
            public static int DisposeCount;
            private readonly IConfiguration _configuration;

            public SampleType(IConfiguration configuration)
            {
                _configuration = configuration;
                CreationCount += 1;
            }

            public IConfiguration GetConfig() => _configuration;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Cleanup
                DisposeCount += 1;
            }
        }
#pragma warning restore S3881

        internal class WithoutCtor
        {
            private readonly string Test = "TEST";
            public string GetTest() => Test;
        }
    }
}
