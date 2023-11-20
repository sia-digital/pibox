using Microsoft.OpenApi.Models;
using PiBox.Api.OpenApi.Filters.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PiBox.Api.OpenApi.Filters.Document
{
    public class ValueObjectDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var toRemove = swaggerDoc.Components.Schemas
                .Where(x => x.Value.Extensions != null && x.Value.Extensions.ContainsKey(Markers.ValueObjectFormatMarker))
                .Select(x => x.Key);
            foreach (var entry in toRemove)
            {
                var schema = swaggerDoc.Components.Schemas[entry];
                schema.Extensions.Remove(Markers.ValueObjectFormatMarker);

                var toEdit = swaggerDoc.Paths.SelectMany(x => x.Value.Operations)
                    .Where(x => x.Value.Parameters?.Any(p => p.Schema?.Reference?.Id == entry) ?? false)
                    .SelectMany(x => x.Value.Parameters.Where(y => y.Schema?.Reference?.Id == entry));
                foreach (var parameter in toEdit)
                {
                    parameter.Schema = schema;
                }

                toEdit = swaggerDoc.Paths.SelectMany(x => x.Value.Parameters)
                    .Where(x => x.Extensions != null && x.Extensions.ContainsKey(Markers.ValueObjectFormatMarker));

                foreach (var parameter in toEdit)
                {
                    parameter.Schema = schema;
                }

                swaggerDoc.Components.Schemas.Remove(entry);
            }
        }
    }
}
