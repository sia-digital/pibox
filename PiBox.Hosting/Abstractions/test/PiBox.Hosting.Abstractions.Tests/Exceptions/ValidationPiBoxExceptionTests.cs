using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Middlewares.Models;
#pragma warning disable SYSLIB0011

namespace PiBox.Hosting.Abstractions.Tests.Exceptions
{
    public class ValidationPiBoxExceptionTests
    {
        [Test]
        public void CanInitWithMessage()
        {
            var validationException = new ValidationPiBoxException("test");
            validationException.HttpStatus.Should().Be(400);
            validationException.Message.Should().Be("test");
            validationException.ValidationErrors.Should().HaveCount(0);
        }

        [Test]
        public void CanInitWithMessageAndValidationErrors()
        {
            var validationException = new ValidationPiBoxException("test", new List<FieldValidationError> { new("test", "test2") });
            validationException.HttpStatus.Should().Be(400);
            validationException.Message.Should().Be("test");
            validationException.ValidationErrors.Should().HaveCount(1);
            var validationError = validationException.ValidationErrors.Single();
            validationError.Field.Should().Be("test");
            validationError.ValidationMessage.Should().Be("test2");
        }
    }
}
