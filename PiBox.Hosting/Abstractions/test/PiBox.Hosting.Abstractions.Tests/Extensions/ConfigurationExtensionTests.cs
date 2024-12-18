using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.Abstractions.Tests.Extensions
{
    public class ConfigurationExtensionTests
    {
        [Test]
        public void CanGetConfigurationSectionAsTypedObject()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "conf:key1", "value1" },
                    { "conf:key2:name", "name" },
                    { "conf:key2:active", "true" },
                    { "conf:key2:age", "13" },
                    { "conf:key2:price", "123.321" },
                    { "conf:key3:0:kind", "test" },
                    { "conf:key3:0:name", "some-name" },
                    { "conf:key3:1:kind", "test" },
                    { "conf:key3:1:name", "some-name2" },
                    { "conf:key4:0", "1" },
                    { "conf:key4:1", "2" },
                    { "conf:key4:2", "3" },

                })
                .Build();
            var result = configuration.GetSection("conf", typeof(TestConfiguration));
            var config = result.Should().NotBeNull().And.BeOfType<TestConfiguration>().Which;
            config.Key1.Should().Be("value1");
            config.Key2.Name.Should().Be("name");
            config.Key2.Active.Should().BeTrue();
            config.Key2.Age.Should().Be(13);
            config.Key2.Price.Should().Be(123.321m);
            config.Key3.Should().HaveCount(2);
            var conf1 = config.Key3[0].Should().BeOfType<TestKindConfiguration>().Which;
            var conf2 = config.Key3[1].Should().BeOfType<TestKindConfiguration>().Which;
            conf1.Kind.Should().Be("Test");
            conf1.Name.Should().Be("some-name");
            conf2.Kind.Should().Be("Test");
            conf2.Name.Should().Be("some-name2");

            config.Key4.Should().HaveCount(3)
                .And.BeEquivalentTo([1, 2, 3]);
            config.Key5.Should().BeNull();

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "conf:key1", "value1" },
                    { "conf:key2:name", "name" },
                    { "conf:key2:active", "true" },
                    { "conf:key2:age", "13" },
                    { "conf:key2:price", "123.321" },
                    // { "conf:key3:0:kind", "test" },
                    { "conf:key3:0:name", "some-name" },
                    { "conf:key3:1:kind", "test" },
                    { "conf:key3:1:name", "some-name2" },
                    { "conf:key4:0", "1" },
                    { "conf:key4:1", "2" },
                    { "conf:key4:2", "3" },

                })
                .Build();
            configuration.Invoking(x => x.GetSection("conf", typeof(TestConfiguration)))
                .Should().Throw<Exception>().And.Message.Should().Contain("Could not find property");

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "conf:key1", "value1" },
                    { "conf:key2:name", "name" },
                    { "conf:key2:active", "true" },
                    { "conf:key2:age", "13" },
                    { "conf:key2:price", "123.321" },
                    { "conf:key3:0:kind", "failure" },
                    { "conf:key3:0:name", "some-name" },
                    { "conf:key3:1:kind", "test" },
                    { "conf:key3:1:name", "some-name2" },
                    { "conf:key4:0", "1" },
                    { "conf:key4:1", "2" },
                    { "conf:key4:2", "3" },

                })
                .Build();
            configuration.Invoking(x => x.GetSection("conf", typeof(TestConfiguration)))
                .Should().Throw<JsonException>().And.Message.Should().Contain("Unknown Type: failure");
        }

        private class TestConfiguration
        {
            public string Key1 { get; set; }
            public TestNestConfiguration Key2 { get; set; }
            public IList<IInterfaceConfiguration> Key3 { get; set; }
            public IList<int> Key4 { get; set; }
            public string Key5 { get; set; }
        }

        private class TestNestConfiguration
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool Active { get; set; }
            public decimal Price { get; set; }
        }

        private interface IInterfaceConfiguration : IKindSpecifier;

        private class TestKindConfiguration : IInterfaceConfiguration
        {
            public string Kind => "Test";
            public string Name { get; set; }
        }
    }
}
