using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PiBox.Api.OpenApi.Filters.Schema
{
    public class ReadOnlyPropertySchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var properties = context.Type.GetProperties().Where(prop => prop.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly == true);
            foreach (var property in properties)
            {
                var schemaId = schema.Properties.Keys.FirstOrDefault(x => string.Equals(x, property.Name, StringComparison.OrdinalIgnoreCase));
                if (schemaId is not null)
                {
                    schema.Properties[schemaId].ReadOnly = true;
                }
            }
        }
    }
}
