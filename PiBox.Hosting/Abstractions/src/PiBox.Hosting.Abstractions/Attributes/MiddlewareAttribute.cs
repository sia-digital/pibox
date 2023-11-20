namespace PiBox.Hosting.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MiddlewareAttribute : Attribute
    {
        public MiddlewareAttribute(int order = int.MaxValue)
        {
            Order = order;
        }

        public int Order { get; }
    }
}
