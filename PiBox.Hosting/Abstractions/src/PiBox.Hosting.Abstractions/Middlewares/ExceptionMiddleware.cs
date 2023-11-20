using System.Diagnostics;
using Chronos.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Attributes;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Hosting.Abstractions.Middlewares
{
    [Middleware(int.MinValue)]
    public sealed class ExceptionMiddleware : ApiMiddleware
    {
        private readonly ILogger _logger;
        private readonly GlobalStatusCodeOptions _globalStatusCodeOptions;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, GlobalStatusCodeOptions globalStatusCodeOptions, IDateTimeProvider dateTimeProvider) : base(next, dateTimeProvider)
        {
            _logger = logger;
            _globalStatusCodeOptions = globalStatusCodeOptions;
        }

        public override async Task Invoke(HttpContext context)
        {
            try
            {
                await Next.Invoke(context).ConfigureAwait(false);
                if (_globalStatusCodeOptions.DefaultStatusCodes.Contains(context.Response.StatusCode))
                    await WriteDefaultResponse(context, context.Response.StatusCode);
            }
            catch (ValidationPiBoxException validationPiBoxException)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error, validationPiBoxException.Message);
                _logger.LogInformation(validationPiBoxException, "Validation PiBox Exception occured");
                var validationErrorResponse = new ValidationErrorResponse(DateTimeProvider.UtcNow,
                    validationPiBoxException.Message, context.TraceIdentifier,
                    validationPiBoxException.ValidationErrors);
                await WriteResponse(context, validationPiBoxException.HttpStatus, validationErrorResponse);
            }
            catch (PiBoxException piBoxException)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error, piBoxException.Message);
                _logger.LogInformation(piBoxException, "PiBox Exception occured");
                await WriteDefaultResponse(context, piBoxException.HttpStatus, piBoxException.Message);
            }
            catch (Exception e)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error, e.Message);
                _logger.LogError(e, "Unhandled Exception occured");
                await WriteDefaultResponse(context, StatusCodes.Status500InternalServerError);
            }
        }
    }
}
