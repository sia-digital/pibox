using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
#pragma warning disable SYSLIB0011

namespace PiBox.Hosting.Abstractions.Tests.Exceptions
{
    public class NotFoundPiBoxExceptionTests
    {
        [Test]
        public void CanInitWithMessage()
        {
            var validationException = new NotFoundPiBoxException("test");
            validationException.HttpStatus.Should().Be(404);
            validationException.Message.Should().Be("test");
        }

        [Test]
        public void CanInitWithEntityAndId()
        {
            var validationException = new NotFoundPiBoxException("test", "123");
            validationException.HttpStatus.Should().Be(404);
            validationException.Message.Should().Be("Could not find test with id '123'");
        }

    }
}
