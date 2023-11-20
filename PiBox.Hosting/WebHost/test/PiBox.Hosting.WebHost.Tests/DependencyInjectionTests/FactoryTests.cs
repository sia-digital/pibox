using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.DependencyInjection;

namespace PiBox.Hosting.WebHost.Tests.DependencyInjectionTests
{
    public class FactoryTests
    {
        private class X { }

        [Test]
        public void CanGetServiceFromFactory()
        {
            var sc = new ServiceCollection();
            sc.AddTransient<X>();
            sc.AddTransient(typeof(IFactory<>), typeof(Factory<>));
            var sp = sc.BuildServiceProvider();
            var xFactory = sp.GetRequiredService<IFactory<X>>();
            var x = xFactory.CreateOrNull();
            x.Should().NotBeNull();
            x = xFactory.Create();
            x.Should().NotBeNull();
        }

        [Test]
        public void CanGetNullServiceFromFactory()
        {
            var sc = new ServiceCollection();
            sc.AddTransient(typeof(IFactory<>), typeof(Factory<>));
            var sp = sc.BuildServiceProvider();
            var xFactory = sp.GetRequiredService<IFactory<X>>();
            var x = xFactory.CreateOrNull();
            x.Should().BeNull();
        }
    }
}
