using PiBox.Hosting.Abstractions.Plugins;

namespace PiBox.Hosting.WebHost.Extensions
{
    internal static class PluginExtensions
    {
        public static string GetPluginName(this Type type)
        {
            var pluginActivateableNames = new List<string>();
            var pluginName = type.Name;
            var interfaceNames = type.GetInterfaces()
                .Where(x => x.IsAssignableTo(typeof(IPluginActivateable)) && x != typeof(IPluginActivateable))
                .Select(x => x.Name.Replace("IPlugin", "").Replace("Configuration", ""));
            var interfaceText = string.Join(", ", interfaceNames);
            pluginActivateableNames.Add($"{pluginName}({interfaceText})");
            return string.Join(", ", pluginActivateableNames);
        }
    }
}
