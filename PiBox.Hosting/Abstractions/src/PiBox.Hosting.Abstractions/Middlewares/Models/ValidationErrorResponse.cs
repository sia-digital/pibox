namespace PiBox.Hosting.Abstractions.Middlewares.Models
{
    public record ValidationErrorResponse : ErrorResponse
    {
        public ValidationErrorResponse(DateTime timestamp, string message, string requestId,
            IEnumerable<FieldValidationError> validationErrors) : base(timestamp, message, requestId)
        {
            this.ValidationErrors = validationErrors;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public IEnumerable<FieldValidationError> ValidationErrors { get; }
    }
}
