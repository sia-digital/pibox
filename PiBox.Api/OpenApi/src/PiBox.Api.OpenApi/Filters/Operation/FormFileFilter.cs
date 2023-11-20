using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PiBox.Api.OpenApi.Filters.Operation
{
    public class FormFileFilter : IOperationFilter
    {

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParams = context.MethodInfo.GetParameters().Where(x => x.ParameterType == typeof(IFormFile))
                .ToList();
            if (!formFileParams.Any()) return;
            var mediaType = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties =
                        formFileParams.ToDictionary(x => x.Name,
                            x => new OpenApiSchema
                            {
                                Description = "Upload File",
                                Type = "file",
                                Format = "binary"
                            }),
                    Required = formFileParams.Select(x => x.Name).ToHashSet()
                }
            };
            operation.RequestBody = new OpenApiRequestBody { Content = { ["multipart/form-data"] = mediaType } };
        }
    }
}
