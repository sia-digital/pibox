using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    [Serializable]
    public class ConflictPiBoxException : PiBoxException
    {
        public ConflictPiBoxException(string message) : base(message)
        {
            HttpStatus = StatusCodes.Status409Conflict;
        }
    }
}
