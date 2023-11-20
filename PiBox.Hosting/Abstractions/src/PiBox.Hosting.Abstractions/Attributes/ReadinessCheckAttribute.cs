namespace PiBox.Hosting.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReadinessCheckAttribute : HealthCheckAttribute
    {
        public ReadinessCheckAttribute(string name) : this(name, new[] { HealthCheckTag.Readiness }) { }
        public ReadinessCheckAttribute(string name, HealthCheckTag tag) : this(name, new[] { HealthCheckTag.Readiness, tag }) { }
        public ReadinessCheckAttribute(string name, IEnumerable<HealthCheckTag> tags) : base(name, tags.Concat(new[] { HealthCheckTag.Readiness }).ToArray()) { }
    }
}
