using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    [Serializable]
    public class PiBoxException : Exception
    {
        protected PiBoxException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {
            HttpStatus = serializationInfo.GetInt32(nameof(HttpStatus));
        }
        public PiBoxException() : base("An unhandled error occured.") { }
        public PiBoxException(string message) : base(message) { }
        public PiBoxException(string message, Exception innerException) : base(message, innerException) { }
        public PiBoxException(int httpStatus) : base("An unhandled error occured.")
        {
            HttpStatus = httpStatus;
        }
        public PiBoxException(string message, int httpStatus) : base(message)
        {
            HttpStatus = httpStatus;
        }

        public PiBoxException(string message, int httpStatus, Exception innerException) : base(message, innerException)
        {
            HttpStatus = httpStatus;
        }

        public int HttpStatus { get; set; } = StatusCodes.Status500InternalServerError;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(HttpStatus), HttpStatus);
        }
    }
}
