using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
#pragma warning disable SYSLIB0011

namespace PiBox.Hosting.Abstractions.Tests.Exceptions
{
    public class PiBoxExceptionTests
    {
        [Test]
        public void CanInitEmpty()
        {
            var exception = new PiBoxException();
            exception.Message.Should().Be("An unhandled error occured.");
            exception.HttpStatus.Should().Be(500);
        }

        [Test]
        public void CanInitWithMessage()
        {
            var exception = new PiBoxException("test");
            exception.Message.Should().Be("test");
            exception.HttpStatus.Should().Be(500);
        }

        [Test]
        public void CanInitWithMessageAndException()
        {
            var exception = new PiBoxException("test", new Exception("inner"));
            exception.Message.Should().Be("test");
            exception.HttpStatus.Should().Be(500);
            exception.InnerException!.Message.Should().Be("inner");
        }

        [Test]
        public void CanInitWithStatus()
        {
            var exception = new PiBoxException(400);
            exception.Message.Should().Be("An unhandled error occured.");
            exception.HttpStatus.Should().Be(400);
        }

        [Test]
        public void CanInitWithMessageAndStatus()
        {
            var exception = new PiBoxException("test", 400);
            exception.Message.Should().Be("test");
            exception.HttpStatus.Should().Be(400);
        }

        [Test]
        public void CanInitWithMessageAndStatusAndException()
        {
            var exception = new PiBoxException("test", 400, new Exception("inner"));
            exception.Message.Should().Be("test");
            exception.HttpStatus.Should().Be(400);
            exception.InnerException!.Message.Should().Be("inner");
        }
    }
}
