using FluentAssertions;
using NUnit.Framework;

namespace PiBox.Plugins.Endpoints.Abstractions.Tests
{
    public class ResourceIdentifierAttributeTests
    {
        [Test]
        public void CanInit()
        {
            var attr = new ResourceIdentifierAttribute();
            attr.Should().NotBeNull();
        }
    }
}
