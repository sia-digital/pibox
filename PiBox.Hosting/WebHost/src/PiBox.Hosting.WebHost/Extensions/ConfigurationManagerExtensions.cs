using Microsoft.Extensions.Configuration;

namespace PiBox.Hosting.WebHost.Extensions
{
    internal static class ConfigurationManagerExtensions
    {
        public static void AddFile(this ConfigurationManager configurationManager, string file, bool optional = true, bool reloadOnChange = true)
        {
            if (file.EndsWith(".json"))
                configurationManager.AddJsonFile(file, optional, reloadOnChange);
            else if (file.EndsWith(".yaml") || file.EndsWith(".yml"))
                configurationManager.AddYamlFile(file, optional, reloadOnChange);
        }
    }
}
