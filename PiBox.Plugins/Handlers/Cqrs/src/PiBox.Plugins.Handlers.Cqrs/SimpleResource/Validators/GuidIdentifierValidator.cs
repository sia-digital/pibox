using FluentValidation;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators
{
    public static class GuidIdentifierValidator
    {
        public static void Validate(AbstractValidator<IGuidIdentifier> validator)
        {
            validator.RuleFor(v => v.Id).NotEmpty();
        }
    }
}
