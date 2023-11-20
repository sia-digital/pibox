using Chronos;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.MongoDb
{
    public class MongoDbPluginTests
    {
        private readonly MongoDbConfiguration _mongoDbConfiguration = new()
        {
            Database = "testDatabase",
            Host = "testHost",
            Port = 9999,
            Password = "testPassword",
            User = "testUser",
            AuthDatabase = "testAuthDatabase"
        };

        [Test]
        public void ConfigureServicesWorks()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            // register dbContext and map as interface
            sc.AddSingleton<IMongoDbInstance, MongoDbInstance>();
            var plugin = new MongoDbPlugin(_mongoDbConfiguration);
            plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();

            var mongoDbConfig = sp.GetRequiredService<MongoDbConfiguration>();
            mongoDbConfig.Should().NotBeNull();
            mongoDbConfig.Database.Should().Be(_mongoDbConfiguration.Database);
            mongoDbConfig.Host.Should().Be(_mongoDbConfiguration.Host);
            mongoDbConfig.AuthDatabase.Should().Be(_mongoDbConfiguration.AuthDatabase);
            mongoDbConfig.Port.Should().Be(_mongoDbConfiguration.Port);
            mongoDbConfig.User.Should().Be(_mongoDbConfiguration.User);
            mongoDbConfig.Password.Should().Be(_mongoDbConfiguration.Password);

            var mongoDbInstance = sp.GetRequiredService<IMongoDbInstance>();
            mongoDbInstance.Should().BeOfType<MongoDbInstance>();

            var readRepo = sp.GetRequiredService<IReadRepository<TestEntity>>();
            readRepo.Should().NotBeNull();
            readRepo.Should().BeOfType<MongoRepository<TestEntity>>();

            var repo = sp.GetRequiredService<IRepository<TestEntity>>();
            repo.Should().NotBeNull();
            repo.Should().BeOfType<MongoRepository<TestEntity>>();
        }

        [Test]
        public void ConfigureHealthChecksWorks()
        {
            var config = new MongoDbConfiguration
            {
                Host = "localhost",
                Port = 27017,
                Database = "test",
                User = "user",
                Password = "pw"
            };
            var plugin = new MongoDbPlugin(config);
            var hcBuilder = Substitute.For<IHealthChecksBuilder>();
            plugin.ConfigureHealthChecks(hcBuilder);
            hcBuilder.Received()
                .Add(Arg.Is<HealthCheckRegistration>(h =>
                    h.Name == "mongo" && h.Tags.Contains(HealthCheckTag.Readiness.Value)));
        }
    }
}
