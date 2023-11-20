using Chronos;
using Microsoft.Extensions.DependencyInjection;
using PiBox.Hosting.Abstractions;
using PiBox.Hosting.Abstractions.DependencyInjection;

namespace PiBox.Testing
{
    public static class TestingDefaults
    {
        public static ServiceCollection ServiceCollection()
        {
            var sc = new ServiceCollection();
            sc.AddDateTimeProvider().AddDateTimeOffsetProvider();
            sc.AddLogging();
            sc.AddTransient(typeof(IFactory<>), typeof(Factory<>));
            sc.AddSingleton<GlobalStatusCodeOptions>();
            return sc;
        }

        public static ServiceProvider ServiceProvider() => ServiceCollection().BuildServiceProvider();
    }
}
