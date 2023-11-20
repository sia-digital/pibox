using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Persistence.InMemory.Tests
{
    public class InMemoryPluginTest
    {
        public InMemoryPluginTest()
        {
            ActivityTestBootstrapper.Setup();
        }

        [Test]
        public void CanRegisterGenericRepository()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton(Substitute.For<IDateTimeProvider>());
            var plugin = new InMemoryPlugin();
            var expectedServices = new Dictionary<Type, Type>
            {
                {typeof(IRepository<SampleModel>), typeof(InMemoryRepository<SampleModel>)}
            };
            plugin.ShouldHaveServices(expectedServices, sc);
        }

        [Test]
        public async Task ReadAndWriteRepositoryAreTheSameData()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton(Substitute.For<IDateTimeProvider>());
            var plugin = new InMemoryPlugin();
            plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();
            var sampleModelRepo = sp.GetRequiredService<IRepository<SampleModel>>();
            await sampleModelRepo.AddAsync(new SampleModel { Name = "Test" });
            var readRepo = sp.GetRequiredService<IReadRepository<SampleModel>>();
            var result = await readRepo.FindAsync(QueryOptions<SampleModel>.Empty);
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var entry = result.Single();
            entry.Should().NotBeNull();
            entry.Name.Should().Be("Test");
        }
    }
}
