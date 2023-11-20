using PiBox.Hosting.Abstractions.Services;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class ImplementationResolverExtensions
    {
        public static List<Type> FindTypes(this IImplementationResolver implementationResolver, Predicate<Type> filter)
            => implementationResolver.FindTypes().Where(f => filter(f)).ToList();
        public static List<object> FindAndResolve(this IImplementationResolver implementationResolver, Predicate<Type> filter)
            => implementationResolver.FindTypes(filter)
                .Select(implementationResolver.ResolveInstance)
                .Where(x => x is not null)
                .ToList();
        public static List<T> FindAndResolve<T>(this IImplementationResolver implementationResolver)
            => implementationResolver.FindAndResolve(f => f.IsAssignableTo(typeof(T)))
                .Where(x => x is not null)
                .OfType<T>().ToList();

    }
}
