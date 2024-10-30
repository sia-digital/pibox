using System.Reflection;

namespace PiBox.Testing.Extensions
{
    public static class ObjectExtensions
    {
        public static T GetInaccessibleValue<T>(this object obj, string name)
        {
            var type = obj.GetType();
            while (type is not null)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                if (field is not null)
                    return (T)field.GetValue(obj)!;
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                if (property is not null)
                    return (T)property.GetValue(obj)!;
                type = type.BaseType;
            }
            throw new ArgumentException($"Could not find field or property '{name}'");
        }
    }
}
