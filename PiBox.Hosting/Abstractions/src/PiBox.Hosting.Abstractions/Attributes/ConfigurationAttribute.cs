namespace PiBox.Hosting.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurationAttribute : Attribute
    {
        private readonly string[] _sections;

        public ConfigurationAttribute(string section)
            : this(section.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {

        }
        public ConfigurationAttribute(params string[] sections)
        {
            _sections = sections;
        }

        public string Section => string.Join(":", _sections);
    }
}
