using System.Net;
using Microsoft.AspNetCore.Http;

namespace PiBox.Hosting.Abstractions
{
    public class GlobalStatusCodeOptions
    {
        public IList<int> DefaultStatusCodes { get; } = new List<int>
        {
            StatusCodes.Status401Unauthorized,
            StatusCodes.Status404NotFound,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status500InternalServerError,
            StatusCodes.Status415UnsupportedMediaType,
            StatusCodes.Status406NotAcceptable
        };

        public GlobalStatusCodeOptions Add(HttpStatusCode statusCode)
        {
            return Add((int)statusCode);
        }

        public GlobalStatusCodeOptions Add(int statusCode)
        {
            DefaultStatusCodes.Add(statusCode);
            return this;
        }

        public GlobalStatusCodeOptions Remove(HttpStatusCode statusCode)
        {
            return Remove((int)statusCode);
        }

        public GlobalStatusCodeOptions Remove(int statusCode)
        {
            DefaultStatusCodes.Remove(statusCode);
            return this;
        }

        public GlobalStatusCodeOptions Clear()
        {
            DefaultStatusCodes.Clear();
            return this;
        }
    }
}
