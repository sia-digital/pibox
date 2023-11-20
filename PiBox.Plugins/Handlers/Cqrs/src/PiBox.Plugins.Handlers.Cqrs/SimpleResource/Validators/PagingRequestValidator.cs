using FluentValidation;
using PiBox.Extensions.Abstractions;

namespace PiBox.Plugins.Handlers.Cqrs.SimpleResource.Validators
{
    public static class PagingRequestValidator
    {
        public static void Validate(AbstractValidator<PagingRequest> validator)
        {
            validator.When(x => x.Page != null, () =>
            {
                validator.RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
            });

            validator.When(x => x.Size != null, () =>
            {
                validator.RuleFor(x => x.Size).GreaterThan(0).LessThanOrEqualTo(100);
            });

            validator.When(x => x.Sort != null, () =>
            {
                validator.RuleFor(x => x.Sort).NotEmpty();
            });

            validator.When(x => x.Filter != null, () =>
            {
                validator.RuleFor(x => x.Filter).NotEmpty();
            });
        }
    }
}
