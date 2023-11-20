using System.Reflection;
using PiBox.Hosting.Abstractions.Plugins;
using PiBox.Hosting.Abstractions.Services;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class PluginActivateableExtensions
    {
        public static List<KeyValuePair<int, T>> FindPlugins<T>(this IImplementationResolver implementationResolver) where T : IPluginActivateable
        {
            return implementationResolver.FindAndResolve<T>().OrderBy(GetSortOrderForPlugin).Select((x, i) => new KeyValuePair<int, T>(i, x)).ToList();
        }

        public static string GetPluginName(this IPluginActivateable pluginActivateable) => pluginActivateable.GetType().Name;

        private static sbyte GetSortOrderForPlugin<T>(T activateable) where T : IPluginActivateable
        {
            var pluginType = activateable.GetType();
            var isHostAssembly = Assembly.GetEntryAssembly() == pluginType.Assembly;
            if (isHostAssembly) return sbyte.MaxValue;
            var isPiBoxPlugin = pluginType.Namespace?.StartsWith("PiBox", StringComparison.OrdinalIgnoreCase) ?? false;
            return (sbyte)(isPiBoxPlugin ? sbyte.MinValue : 0);
        }
    }
}
