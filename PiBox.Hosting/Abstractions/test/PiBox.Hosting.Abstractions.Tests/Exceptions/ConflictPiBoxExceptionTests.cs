using System.Runtime.Serialization.Formatters.Binary;
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

        [Test]
        public void CanSerializeException()
        {
            var exception = new ConflictPiBoxException("the message");
            using var serStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();
            binFormatter.Serialize(serStream, exception);
            var bytes = serStream.GetBuffer();
            using var desStream = new MemoryStream(bytes);
            var newException = binFormatter.Deserialize(desStream) as ConflictPiBoxException;
            newException.Should().BeEquivalentTo(exception);
        }
    }
}
