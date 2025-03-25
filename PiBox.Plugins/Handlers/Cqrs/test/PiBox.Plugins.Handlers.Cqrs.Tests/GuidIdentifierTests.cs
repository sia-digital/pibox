using FluentAssertions;
using NUnit.Framework;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Models;

namespace PiBox.Plugins.Handlers.Cqrs.Tests
{
    public class GuidIdentifierTests
    {
        [Test]
        public void GuidIdentifierNewShouldNotBeEqualToGuidIdentifierNew()
        {
            GuidIdentifier.New.Id.Should().NotBe(GuidIdentifier.New.Id);
        }

        [Test]
        public void GuidIdentifierParseShouldBeWorking()
        {
            GuidIdentifier.Parse("05a82a17-5a7f-4f28-8ff9-37f35c2cfb5f").Id.Should().Be(Guid.Parse("05a82a17-5a7f-4f28-8ff9-37f35c2cfb5f"));
        }

        [Test]
        public void EmptyGuidIdentifierIsAnEmptyGuid()
        {
            var empty = GuidIdentifier.Empty;
            empty.Should().Be(GuidIdentifier.Empty);
            empty.Id.Should().Be(Guid.Empty);
        }

        [Test]
        public void CanResetTheIdOfAnGuidIdentifier()
        {
            var empty = GuidIdentifier.Empty;
            var newGuid = Guid.NewGuid();
            empty.Id = newGuid;
            empty.Id.Should().Be(newGuid);
        }
    }
}
