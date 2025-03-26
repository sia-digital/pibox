using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using PiBox.Extensions.Abstractions;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators;

namespace PiBox.Plugins.Handlers.Cqrs.Tests.SimpleResource.Validators
{
    public class ValidationExtensionsTests
    {
        private const string DefaultUserMessage = "One or more validations have failed.";
        private readonly IValidator<PagingRequest> _validator = new GenericValidator<PagingRequest>(PagingRequestValidator.Validate);

        [Test]
        public async Task ThrowsWhenRequestIsNull()
        {
            var validationException = await _validator
                .Invoking(async x => await x.ValidateOrThrowAsync(null, CancellationToken.None)).Should()
                .ThrowAsync<ValidationPiBoxException>();
            validationException.Message.Should().Be(DefaultUserMessage);
            validationException.ValidationErrors.Should().HaveCount(1);
            validationException.ValidationErrors[0].Field.Should().Be("request");
            validationException.ValidationErrors[0].ValidationMessage.Should().Be("Cannot pass null value");
        }

        [Test]
        public async Task DoesNothingWhenEverythingIsGood()
        {
            await _validator.ValidateOrThrowAsync(new PagingRequest(), CancellationToken.None);
        }

        [Test]
        public async Task ThrowsWhenValidationIsInvalid()
        {
            var validationException = await _validator.Invoking(async x =>
                    await x.ValidateOrThrowAsync(new PagingRequest(0), CancellationToken.None))
                .Should().ThrowAsync<ValidationPiBoxException>();
            validationException.Message.Should().Be(DefaultUserMessage);
            validationException.ValidationErrors.Should().HaveCount(1);
            validationException.ValidationErrors[0].Field.Should().Be("Size");
            validationException.ValidationErrors[0].ValidationMessage.Should().Be("'Size' must be greater than '0'.");
        }

    }
}
