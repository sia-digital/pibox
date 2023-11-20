using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Attributes;

namespace PiBox.Hosting.Abstractions.Tests.Attributes
{
    public class HealthCheckAttributesTest
    {
        private static void AssertHealthCheckTags(HealthCheckAttribute healthCheckAttribute, params HealthCheckTag[] tags)
        {
            healthCheckAttribute.Should().NotBeNull();
            healthCheckAttribute.Tags.Should().NotBeEmpty();
            foreach (var tag in tags)
            {
                healthCheckAttribute.Tags.Should().Contain(tag);
            }
        }

        [Test]
        public void ReadinessCheckCanBeInitialized()
        {
            var readinessCheck = new ReadinessCheckAttribute("test");
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Readiness);
            var tags = new[] { HealthCheckTag.From("testing"), HealthCheckTag.From("tester") };
            readinessCheck = new ReadinessCheckAttribute("test", tags);
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Readiness, tags[0], tags[1]);

            readinessCheck = new ReadinessCheckAttribute("test", HealthCheckTag.From("mytag"));
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Readiness, HealthCheckTag.From("mytag"));
        }

        [Test]
        public void LivenessCheckCanBeInitialized()
        {
            var readinessCheck = new LivenessCheckAttribute("test");
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Liveness);
            var tags = new[] { HealthCheckTag.From("testing"), HealthCheckTag.From("tester") };
            readinessCheck = new LivenessCheckAttribute("test", tags);
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Liveness, tags[0], tags[1]);

            readinessCheck = new LivenessCheckAttribute("test", HealthCheckTag.From("mytag"));
            readinessCheck.Name.Should().Be("test");
            AssertHealthCheckTags(readinessCheck, HealthCheckTag.Liveness, HealthCheckTag.From("mytag"));
        }
    }
}
