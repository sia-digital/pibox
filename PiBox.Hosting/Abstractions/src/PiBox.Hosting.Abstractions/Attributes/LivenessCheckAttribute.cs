namespace PiBox.Hosting.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LivenessCheckAttribute : HealthCheckAttribute
    {
        public LivenessCheckAttribute(string name) : this(name, new[] { HealthCheckTag.Liveness }) { }
        public LivenessCheckAttribute(string name, HealthCheckTag tag) : this(name, new[] { HealthCheckTag.Liveness, tag }) { }
        public LivenessCheckAttribute(string name, IEnumerable<HealthCheckTag> tags) : base(name, tags.Concat(new[] { HealthCheckTag.Liveness }).ToArray()) { }
    }
}
