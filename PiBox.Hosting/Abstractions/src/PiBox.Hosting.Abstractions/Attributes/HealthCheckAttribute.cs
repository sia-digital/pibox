namespace PiBox.Hosting.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class HealthCheckAttribute : Attribute
    {
        protected HealthCheckAttribute(string name, HealthCheckTag[] tags)
        {
            Name = name;
            Tags = tags;
        }

        public string Name { get; }
        public HealthCheckTag[] Tags { get; }
    }
}
