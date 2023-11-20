using Microsoft.Extensions.Logging;

namespace PiBox.Testing.Assertions
{
    /// <summary>
    /// Stolen from https://alessio.franceschelli.me/posts/dotnet/how-to-test-logging-when-using-microsoft-extensions-logging/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FakeLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object>> Properties, Exception
            Exception)> Entries
        { get; } =
            new();

        public IDisposable BeginScope<TState>(TState state) => throw new NotSupportedException();

        public bool IsEnabled(LogLevel logLevel) => throw new NotSupportedException();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            // These are relying on an internal implementation detail, they will break!
            var message = state!.ToString();
            var properties = state as IReadOnlyList<KeyValuePair<string, object>>;

            Entries.Add((logLevel, message, properties, exception)!);
        }
    }
}
