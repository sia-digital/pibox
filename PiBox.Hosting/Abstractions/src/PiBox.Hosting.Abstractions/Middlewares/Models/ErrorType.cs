namespace PiBox.Hosting.Abstractions.Middlewares.Models
{
    public enum ErrorType
    {
        Validation,
        Exception,
        AggregateException,
        TimeoutException,
        IoException,
        ResourceNotFound,
        ResourceConflict,
        Unauthorized,
        Forbidden,
        Client,
        Unhandled
    }
}
