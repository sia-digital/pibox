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
    }
}
