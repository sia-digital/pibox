using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    public class ConflictPiBoxException : PiBoxException
    {
        public ConflictPiBoxException(string message) : base(message)
        {
            HttpStatus = StatusCodes.Status409Conflict;
        }
    }
}
