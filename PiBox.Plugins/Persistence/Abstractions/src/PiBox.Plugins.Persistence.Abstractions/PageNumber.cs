using Vogen;

namespace PiBox.Plugins.Persistence.Abstractions
{
    [ValueObject<int>(comparison: ComparisonGeneration.Omit)]
    [Instance("Default", 0)]
    public readonly partial struct PageNumber
    {
        private static Validation Validate(int input)
        {
            return input >= 0 ? Validation.Ok : Validation.Invalid($"{nameof(PageNumber)} must be greater or equal to zero.");
        }

        public static PageNumber FromNullable(int? input) => input.HasValue ? From(input.Value) : Default;
    }
}
