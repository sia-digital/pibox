namespace PiBox.Plugins.Persistence.Abstractions.Exceptions
{
    public sealed class QueryOptionsException : Exception
    {
        public QueryOptionsException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
