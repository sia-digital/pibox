using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Persistence.Smb.Tests
{
    public class SmbPluginTests
    {
        [Test]
        public void PluginConfiguresServices()
        {
            var sc = TestingDefaults.ServiceCollection();
            sc.AddSingleton<SmbStorageConfiguration>();
            var plugin = new SmbStoragePlugin();
            plugin.ConfigureServices(sc);
            var expectedServices = new Dictionary<Type, Type>
            {
                {typeof(ISmbStorage), typeof(SmbStorage)}
            };
            plugin.ShouldHaveServices(expectedServices, sc);
        }
    }
}
