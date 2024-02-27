using System.Reflection;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class AssemblyExtensions
    {
        public static bool HasType(this Assembly assembly, Predicate<Type> typePredicate)
        {
            return assembly.GetTypes().Any(t => typePredicate(t));
        }
    }
}
