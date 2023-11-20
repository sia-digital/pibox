using Vogen;

namespace PiBox.Plugins.Persistence.Abstractions
{
    [ValueObject<int>(comparison: ComparisonGeneration.Omit)]
    [Instance("Default", 25)]
    public readonly partial struct PageSize
    {
        private static Validation Validate(int input)
        {
            return input > 0 ? Validation.Ok : Validation.Invalid($"{nameof(PageSize)} must be greater than zero.");
        }

        public static PageSize FromNullable(int? input) => input.HasValue ? From(input.Value) : Default;
    }
}
