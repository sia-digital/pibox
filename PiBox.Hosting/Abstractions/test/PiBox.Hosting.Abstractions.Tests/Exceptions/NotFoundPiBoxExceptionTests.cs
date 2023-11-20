using System.Runtime.Serialization.Formatters.Binary;
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

        [Test]
        public void CanSerializeException()
        {
            var exception = new NotFoundPiBoxException("the message");
            using var serStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();
            binFormatter.Serialize(serStream, exception);
            var bytes = serStream.GetBuffer();
            using var desStream = new MemoryStream(bytes);
            var newException = binFormatter.Deserialize(desStream) as NotFoundPiBoxException;
            newException.Should().BeEquivalentTo(exception);
        }
    }
}
