using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Hosting.Abstractions.Exceptions
{
    [Serializable]
    public class ValidationPiBoxException : PiBoxException
    {
        protected ValidationPiBoxException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {
            var validationErrors = serializationInfo.GetString(nameof(ValidationErrors))!;
            ValidationErrors = JsonSerializer.Deserialize<IList<FieldValidationError>>(validationErrors);
        }
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

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ValidationErrors), JsonSerializer.Serialize(ValidationErrors));
        }
    }
}
