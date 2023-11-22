using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    public class NotFoundPiBoxException : PiBoxException
    {
        public NotFoundPiBoxException(string message) : base(message)
        {
            HttpStatus = StatusCodes.Status404NotFound;
        }

        public NotFoundPiBoxException() : base("not found")
        {
            HttpStatus = StatusCodes.Status404NotFound;
        }

        public NotFoundPiBoxException(string entity, string id) : this($"Could not find {entity} with id '{id}'")
        {
        }
    }
}
