using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;

namespace PiBox.Hosting.WebHost.Tests
{
    public class ImplementationResolverExtensionsTests
    {
        [Test]
        public void FindPluginsShouldReturnPluginsInCorrectOrder()
        {
            var implementationResolver = Substitute.For<IImplementationResolver>();
            var plugins = new List<Type> { typeof(TestPlugin), typeof(WebHostPlugin) };
            implementationResolver.FindTypes().Returns(plugins);
            implementationResolver.ResolveInstance(typeof(WebHostPlugin)).Returns(new WebHostPlugin());
            implementationResolver.ResolveInstance(typeof(TestPlugin)).Returns(new TestPlugin());

            var foundPlugins = implementationResolver.FindPlugins<IPluginActivateable>();
            foundPlugins[0].Key.Should().Be(0);
            foundPlugins[0].Value.Should().BeOfType<TestPlugin>();
            foundPlugins[1].Key.Should().Be(1);
            foundPlugins[1].Value.Should().BeOfType<WebHostPlugin>();
        }

        [Test]
        public void CanGetNameForPluginActivateable()
        {
            var name = new TestPlugin().GetPluginName();
            name.Should().Be("TestPlugin");
        }
    }

    public class WebHostPlugin : IPluginActivateable { }
    public class TestPlugin : IPluginActivateable { }
}
