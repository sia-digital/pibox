using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using PiBox.Hosting.Abstractions.Extensions;

namespace PiBox.Hosting.WebHost.Formatters
{
    public class YamlInputFormatter : TextInputFormatter
    {
        public YamlInputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(CustomMediaTypes.ApplicationYaml);
            SupportedMediaTypes.Add(CustomMediaTypes.TextYaml);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var httpRequest = (context ?? throw new ArgumentNullException(nameof(context))).HttpContext.Request;
            using var reader = context.ReaderFactory(httpRequest.Body, encoding);
            var targetType = context.ModelType;
            try
            {
                var content = await reader.ReadToEndAsync();
                var model = content.Deserialize(targetType, SerializationMethod.Yaml);
                return await InputFormatterResult.SuccessAsync(model);
            }
            catch (Exception)
            {
                return await InputFormatterResult.FailureAsync();
            }
        }
    }
}
