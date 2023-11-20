
using Microsoft.Net.Http.Headers;

namespace PiBox.Hosting.WebHost.Formatters
{
    public static class CustomMediaTypes
    {
        public static readonly MediaTypeHeaderValue ApplicationYaml =
            MediaTypeHeaderValue.Parse("application/yaml").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue TextYaml =
            MediaTypeHeaderValue.Parse("text/yaml").CopyAsReadOnly();
    }
}
