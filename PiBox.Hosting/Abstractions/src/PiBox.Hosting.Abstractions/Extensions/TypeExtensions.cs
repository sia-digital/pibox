using System.Reflection;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class TypeExtensions
    {
        public static bool Implements(this Type type, Type implementationType)
        {
            var currentChild = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            while (currentChild != typeof(object))
            {
                if (implementationType == currentChild
                    || HasAnyInterfaces(currentChild, implementationType)
                    || implementationType == currentChild.BaseType)
                    return true;

                currentChild = currentChild.BaseType is { IsGenericType: true }
                    ? currentChild.BaseType.GetGenericTypeDefinition()
                    : currentChild.BaseType;

                if (currentChild == null)
                    return false;
            }
            return false;
        }
        public static bool Implements<T>(this Type type) => type.Implements(typeof(T));
        public static bool HasAttribute<T>(this Type type) where T : Attribute => type.GetCustomAttribute<T>() is not null;
        public static T GetAttribute<T>(this Type type) where T : Attribute => type.GetCustomAttribute<T>();
        public static IList<T> GetAttributes<T>(this Type type) where T : Attribute => type.GetCustomAttributes<T>().ToList();
        private static bool HasAnyInterfaces(Type type, Type implementationType)
        {
            return type.GetInterfaces()
                .Any(childInterface =>
                {
                    if (childInterface == implementationType)
                        return true;
                    var currentInterface = childInterface.IsGenericType
                        ? childInterface.GetGenericTypeDefinition()
                        : childInterface;

                    return currentInterface == implementationType;
                });
        }
    }
}
