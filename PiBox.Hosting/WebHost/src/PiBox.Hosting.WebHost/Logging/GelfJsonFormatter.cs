using System.Globalization;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace PiBox.Hosting.WebHost.Logging
{
    /// <summary>
    /// An <see cref="ITextFormatter"/> that writes events in a GELF JSON format, for consumption in environments
    /// /// without message template support. Message templates are rendered into text and a hashed event id is included.
    /// </summary>
    public class GelfJsonFormatter : ITextFormatter
    {
        /// <summary>
        /// Format the log event into the GELF output. Subsequent events will be newline-delimited.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var json = JsonSerializer.Serialize(GetLogEntry(logEvent));
            output.WriteLine(json);
        }

        private static IDictionary<string, object> GetLogEntry(LogEvent logEvent)
        {
            var message = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
            var logDic = new Dictionary<string, object>
            {
                { "timestamp", logEvent.Timestamp.UtcDateTime.ToString("O") },
                { "short_message", message[..Math.Min(message.Length, 128)] },
                { "full_message", message },
                { "level_name", GetLevelName(logEvent.Level) },
                { "level", GetSyslogLevel(logEvent.Level) }
            };
            if (logEvent.Exception != null)
                logDic.Add("exception", logEvent.Exception.ToString());

            foreach ((var key, var value) in logEvent.Properties)
            {
                var propertyName = GetPropertyName(key);
                if (logDic.ContainsKey(propertyName)) continue; // skip existing keys
                logDic.Add(propertyName, value.ToString());
            }

            return logDic;
        }

        private static string GetPropertyName(string propertyKey)
        {
            return propertyKey switch
            {
                "SourceContext" => "logger_name",
                "ThreadName" => "thread_name",
                "ThreadId" => "thread_id",
                _ => propertyKey
            };
        }

        private static int GetSyslogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => 7,
                LogEventLevel.Debug => 7,
                LogEventLevel.Information => 6,
                LogEventLevel.Warning => 4,
                LogEventLevel.Error => 3,
                LogEventLevel.Fatal => 2,
                _ => 5 // use notice as default
            };
        }

        private static string GetLevelName(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => "VERBOSE",
                LogEventLevel.Debug => "DEBUG",
                LogEventLevel.Information => "INFO",
                LogEventLevel.Warning => "WARN",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "FATAL",
                _ => level.ToString()
            };
        }
    }
}
