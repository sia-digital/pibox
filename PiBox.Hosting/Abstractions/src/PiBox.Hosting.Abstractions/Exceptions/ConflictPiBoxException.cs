using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    [Serializable]
    public class ConflictPiBoxException : PiBoxException
    {
        protected ConflictPiBoxException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context) { }

        public ConflictPiBoxException(string message) : base(message)
        {
            HttpStatus = StatusCodes.Status409Conflict;
        }
    }
}
