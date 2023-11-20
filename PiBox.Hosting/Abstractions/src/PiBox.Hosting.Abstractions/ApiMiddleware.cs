using System.Diagnostics.CodeAnalysis;
using Chronos.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Hosting.Abstractions
{
    /// <summary>
    /// Abstract skeleton for asp net core middlewares
    /// No testing needed.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class ApiMiddleware
    {
        protected RequestDelegate Next { get; }
        protected IDateTimeProvider DateTimeProvider { get; }

        protected ApiMiddleware(RequestDelegate next, IDateTimeProvider dateTimeProvider)
        {
            Next = next;
            DateTimeProvider = dateTimeProvider;
        }

        public abstract Task Invoke(HttpContext context);

        protected static Task WriteResponse<T>(HttpContext context, int statusCode, T result)
        {
            if (context.Response.HasStarted) return Task.CompletedTask;
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(result);
        }

        protected Task WriteDefaultResponse(HttpContext context, int statusCode, string message = null) =>
            WriteResponse(context, statusCode, new ErrorResponse(DateTimeProvider.UtcNow,
                message ?? ReasonPhrases.GetReasonPhrase(statusCode), context.TraceIdentifier));
    }
}
