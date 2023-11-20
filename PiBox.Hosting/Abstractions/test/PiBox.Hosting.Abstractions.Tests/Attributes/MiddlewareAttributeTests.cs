using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.Abstractions.Tests.Attributes
{
    public class MiddlewareAttributeTests
    {
        [Test]
        public void CanSpecifyMiddlewareOrder()
        {
            var attr = new MiddlewareAttribute(101);
            attr.Order.Should().Be(101);
            attr.Should().BeAssignableTo<Attribute>();
            var attrUsage = attr.GetType().GetAttribute<AttributeUsageAttribute>();
            attrUsage.Should().NotBeNull();
            attrUsage.ValidOn.Should().Be(AttributeTargets.Class);
        }
    }
}
