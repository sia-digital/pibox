namespace PiBox.Hosting.Abstractions.Middlewares.Models
{
    public record ErrorResponse(DateTime Timestamp, string Message, string RequestId);
}
