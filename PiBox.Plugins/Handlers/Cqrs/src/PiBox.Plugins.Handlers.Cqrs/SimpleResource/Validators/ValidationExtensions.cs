using FluentValidation;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators
{
    public static class ValidationExtensions
    {
        public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T request, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ValidationPiBoxException("One or more validations have failed.", new List<FieldValidationError> { new("request", "Cannot pass null value") });
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (result.IsValid) return;

            var validationErrors = result.Errors
                .Select(x => new FieldValidationError(x.PropertyName, x.ErrorMessage))
                .ToArray();

            throw new ValidationPiBoxException("One or more validations have failed.", validationErrors);
        }
    }
}
