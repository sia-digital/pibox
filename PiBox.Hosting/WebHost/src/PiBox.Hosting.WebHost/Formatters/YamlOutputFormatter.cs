using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.WebHost.Formatters
{
    public class YamlOutputFormatter : TextOutputFormatter
    {
        public YamlOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(CustomMediaTypes.ApplicationYaml);
            SupportedMediaTypes.Add(CustomMediaTypes.TextYaml);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var httpResponse = (context ?? throw new ArgumentNullException(nameof(context))).HttpContext.Response;
            await using var writer = context.WriterFactory(httpResponse.Body, selectedEncoding);
            var content = context.Object!.Serialize(SerializationMethod.Yaml);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }
    }
}
