using FluentValidation;

namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators
{
    public interface ICreateValidator<TResource> : IBaseValidator
    {
        void ValidateOnCreate(AbstractValidator<TResource> validator);
    }
}
