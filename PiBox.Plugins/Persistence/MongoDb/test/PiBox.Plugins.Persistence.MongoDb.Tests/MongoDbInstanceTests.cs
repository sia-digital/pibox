using FluentAssertions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NUnit.Framework;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.MongoDb
{
    [TestFixture]
    public class MongoDbInstanceTests
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
        public void MongoDbInstanceConstructor()
        {
            var instance = new MongoDbInstance(_mongoDbConfiguration);
            instance.Client.Settings.Credential.Username.Should().Be("testUser");
            instance.Client.Settings.Credential.Evidence.Should().Be(new PasswordEvidence("testPassword"));
            instance.Client.Settings.Server.Host.Should().Be("testHost");
            instance.Client.Settings.Server.Port.Should().Be(9999);
            instance.Database.DatabaseNamespace.DatabaseName.Should().Be("testDatabase");
        }

        [Test]
        public void MetricCounterGetsIncremented()
        {
            using var metricsCollector = new TestMetricsCollector("mongodb_driver_connection_opened_event_total");
            metricsCollector.CollectedMetrics.Should().BeEmpty();
            metricsCollector.Instruments.Should().BeEmpty();
            MongoDbInstance.IncrementCounter(new ConnectionOpenedEvent());
            metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
            metricsCollector.Instruments.Should().Contain("mongodb_driver_connection_opened_event_total");
            MongoDbInstance.IncrementCounter(new ConnectionOpenedEvent());
            metricsCollector.GetSum().Should().Be(2);
        }

        [Test]
        public void ToSnakeCaseWorks()
        {
            MongoDbInstance.ToSnakeCase("MyFancyName").Should().Be("my_fancy_name");
        }

        [Test]
        public void TestGetCollectionFor()
        {
            var instance = new MongoDbInstance(_mongoDbConfiguration);
            var mongoCollection = instance.GetCollectionFor<TestEntity>();
            mongoCollection.Should().BeAssignableTo<IMongoCollection<TestEntity>>().And.NotBeNull();
            mongoCollection.CollectionNamespace.CollectionName.Should().Be(nameof(TestEntity));
        }
    }
}
