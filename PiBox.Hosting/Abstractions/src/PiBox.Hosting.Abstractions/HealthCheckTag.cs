using Vogen;

namespace PiBox.Hosting.Abstractions
{
    [ValueObject<string>(comparison: ComparisonGeneration.Omit)]
    [Instance("Readiness", "readiness")]
    [Instance("Liveness", "liveness")]
    public readonly partial struct HealthCheckTag
    {
        private static Validation Validate(string input)
        {
            return input?.Length > 0 ? Validation.Ok : Validation.Invalid($"{nameof(HealthCheckTag)} must be not null or empty");
        }
    }
}
