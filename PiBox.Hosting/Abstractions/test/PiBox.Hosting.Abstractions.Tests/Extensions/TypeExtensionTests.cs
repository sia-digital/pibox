using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.Abstractions.Tests.Extensions
{
    public class TypeExtensionTests
    {
        [Test]
        public void CanHandleAttributes()
        {
            var type = typeof(TestClass);
            type.HasAttribute<CustomAttribute>().Should().BeTrue();
            var attribute = type.GetAttribute<CustomAttribute>();
            attribute.Should().NotBeNull();
            attribute!.Name.Should().Be("TEST");
            var attributes = type.GetAttributes<CustomAttribute>();
            attributes.Should().HaveCount(1);
        }

        [Test]
        public void CanCheckForGenericImplementations()
        {
            var type = typeof(TestClass);
            type.Implements(typeof(IGeneric<>)).Should().BeTrue();
            type.Implements(typeof(IGeneric<int>)).Should().BeTrue();
            type.Implements(typeof(Generic<>)).Should().BeTrue();
            type.Implements(typeof(Generic<int>)).Should().BeTrue();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class CustomAttribute : Attribute
    {
        public CustomAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    // ReSharper disable once UnusedTypeParameter
#pragma warning disable S2326
    internal interface IGeneric<T> { }
#pragma warning restore S2326
    internal class Generic<T> : IGeneric<T> { }

    [Custom("TEST")]
    internal class TestClass : Generic<int> { }
}
