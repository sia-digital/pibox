using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    public class PiBoxException : Exception
    {
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
    }
}
