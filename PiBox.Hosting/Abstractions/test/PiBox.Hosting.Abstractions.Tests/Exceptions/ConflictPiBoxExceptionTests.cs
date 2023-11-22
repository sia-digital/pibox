using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
#pragma warning disable SYSLIB0011

namespace PiBox.Hosting.Abstractions.Tests.Exceptions
{
    public class ConflictPiBoxExceptionTests
    {
        [Test]
        public void CanInitWithMessage()
        {
            var validationException = new ConflictPiBoxException("test");
            validationException.HttpStatus.Should().Be(409);
            validationException.Message.Should().Be("test");
        }
    }
}
