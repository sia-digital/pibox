using Microsoft.AspNetCore.Http;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    public class ValidationPiBoxException : PiBoxException
    {
        public ValidationPiBoxException(string message) : base(message)
        {
            HttpStatus = StatusCodes.Status400BadRequest;
        }
        public ValidationPiBoxException(string message, IList<FieldValidationError> validationErrors) : base(message)
        {
            HttpStatus = StatusCodes.Status400BadRequest;
            ValidationErrors = validationErrors;
        }

        public IList<FieldValidationError> ValidationErrors { get; init; } = new List<FieldValidationError>();
    }
}
