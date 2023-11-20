using System.Reflection;

namespace PiBox.Testing.Extensions
{
    public static class ObjectExtensions
    {
        public static T GetInaccessibleValue<T>(this object obj, string name)
        {
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            if (field is not null)
                return (T)field.GetValue(obj)!;
            var property = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            if (property is not null)
                return (T)property.GetValue(obj)!;
            throw new ArgumentException($"Could not find field or property '{name}'");
        }
    }
}
