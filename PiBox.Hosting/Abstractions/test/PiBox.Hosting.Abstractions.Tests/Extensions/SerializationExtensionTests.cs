using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.Abstractions.Tests.Extensions
{
    public class SerializationExtensionTests
    {
        private class Sample
        {
            // vogen type...
            public HealthCheckTag HealthCheckTag { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void CanSerializeAndDeserializeJson()
        {
            var sample = new Sample { HealthCheckTag = HealthCheckTag.Liveness, Name = "test" };
            var serialized = sample.Serialize();
            var deserialized = serialized.Deserialize<Sample>();

            deserialized.Should().BeEquivalentTo(sample);

            var obj = serialized.Deserialize(typeof(Sample));
            obj.Should().BeOfType<Sample>();
            (obj as Sample).Should().BeEquivalentTo(sample);
        }

        [Test]
        public void CanSerializeAndDeserializeYaml()
        {
            var sample = new Sample { HealthCheckTag = HealthCheckTag.Liveness, Name = "test" };
            var serialized = sample.Serialize(SerializationMethod.Yaml);
            var withSchema = "$schema: bla.json" + Environment.NewLine + serialized;
            var deserialized = serialized.Deserialize<Sample>(SerializationMethod.Yaml);
            var deserializedWithSchema = withSchema.Deserialize<Sample>(SerializationMethod.Yaml);

            deserialized.Should().BeEquivalentTo(sample);
            deserializedWithSchema.Should().BeEquivalentTo(sample);

            var obj = serialized.Deserialize(typeof(Sample), SerializationMethod.Yaml);
            obj.Should().BeOfType<Sample>();
            (obj as Sample).Should().BeEquivalentTo(sample);
        }

        [Test]
        public void CanSerializeAndDeserializeObjectsWithKindSpecifiers()
        {
            var sample = new SampleWithKindSpecifiers
            {
                Samples = [new SampleConf { Name = "one" }, new Sample2Conf { Name = "two" }]
            };
            var serialized = sample.Serialize();
            var deserialized = serialized.Deserialize<SampleWithKindSpecifiers>();
            deserialized.Samples.Should().HaveCount(2);
            deserialized.Samples[0].Name.Should().Be("one");
            deserialized.Samples[0].Kind.Should().Be("one");
            deserialized.Samples[1].Name.Should().Be("two");
            deserialized.Samples[1].Kind.Should().Be("two");

            serialized = sample.Serialize(SerializationMethod.Yaml);
            deserialized = serialized.Deserialize<SampleWithKindSpecifiers>(SerializationMethod.Yaml);
            deserialized.Samples.Should().HaveCount(2);
            deserialized.Samples[0].Name.Should().Be("one");
            deserialized.Samples[0].Kind.Should().Be("one");
            deserialized.Samples[1].Name.Should().Be("two");
            deserialized.Samples[1].Kind.Should().Be("two");
        }

        private interface ISample : IKindSpecifier
        {
            string Name { get; }
        }

        private class SampleConf : ISample
        {
            public string Kind => "one";
            public string Name { get; set; }
        }

        private class Sample2Conf : ISample
        {
            public string Kind => "two";
            public string Name { get; set; }
        }

        private class SampleWithKindSpecifiers
        {
            public IList<ISample> Samples { get; set; }
        }
    }
}
