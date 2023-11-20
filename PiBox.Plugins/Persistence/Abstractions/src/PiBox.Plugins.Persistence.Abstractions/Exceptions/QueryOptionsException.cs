using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace PiBox.Plugins.Persistence.Abstractions.Exceptions
{
    [Serializable]
    public sealed class QueryOptionsException : Exception
    {
        [ExcludeFromCodeCoverage]
        private QueryOptionsException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
        public QueryOptionsException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
