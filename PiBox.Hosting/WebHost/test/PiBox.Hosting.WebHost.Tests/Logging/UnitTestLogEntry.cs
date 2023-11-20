using System.Globalization;

namespace PiBox.Hosting.WebHost.Tests.Logging
{
    internal class UnitTestLogEntry : Dictionary<string, object>
    {
        internal bool HasTimestamp() => ContainsKey("timestamp");
        internal string GetShortMessage() => this["short_message"] as string;
        internal string GetFullMessage() => this["full_message"] as string;
        internal string GetLevelName() => this["level_name"] as string;
        internal int GetLevel() => Convert.ToInt32(this["level"], CultureInfo.InvariantCulture);
        internal string GetException() => this["exception"] as string;
        internal string GetLoggerName() => this["logger_name"] as string;
        internal T GetProperty<T>(string property) => (T)Convert.ChangeType(this[property], typeof(T), CultureInfo.InvariantCulture);
    }
}
