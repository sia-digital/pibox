using FluentValidation;

namespace PiBox.Plugins.Handlers.Abstractions.Cqrs.Validators
{
    public interface IGetListValidator<TResource> : IBaseValidator
    {
        void ValidateOnGetList(AbstractValidator<TResource> validator);
    }
}
