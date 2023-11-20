using System.Net;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Plugins.Endpoints.RestResourceEntity
{
    public class GlobalResponseOptions
    {
        public IDictionary<HttpStatusCode, Type> DefaultResponses { get; } = new Dictionary<HttpStatusCode, Type>
        {
            {HttpStatusCode.Unauthorized, typeof(ErrorResponse)},
            {HttpStatusCode.Forbidden, typeof(ErrorResponse)},
            {HttpStatusCode.InternalServerError, typeof(ErrorResponse)},
            {HttpStatusCode.BadRequest, typeof(ValidationErrorResponse)},
        }!;

        public GlobalResponseOptions Add<T>(HttpStatusCode statusCode) => Add(statusCode, typeof(T));
        public GlobalResponseOptions Add(HttpStatusCode statusCode) => Add(statusCode, null);

        public GlobalResponseOptions Add(HttpStatusCode statusCode, Type responseType)
        {
            DefaultResponses.Add(statusCode, responseType);
            return this;
        }

        public GlobalResponseOptions Remove(HttpStatusCode statusCode)
        {
            DefaultResponses.Remove(statusCode);
            return this;
        }
    }
}
