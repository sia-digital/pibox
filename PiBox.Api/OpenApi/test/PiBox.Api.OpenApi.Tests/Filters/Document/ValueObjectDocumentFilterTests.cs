using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using PiBox.Api.OpenApi.Filters.Common;
using PiBox.Api.OpenApi.Filters.Document;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PiBox.Api.OpenApi.Tests.Filters.Document
{
    public class ValueObjectDocumentFilterTests
    {
        private readonly IDocumentFilter _documentFilter = new ValueObjectDocumentFilter();

        [Test]
        public void AdjustsSchemaForValueObjects()
        {
            var openApiDoc = new OpenApiDocument
            {
                Components = new OpenApiComponents
                {
                    Schemas = new Dictionary<string, OpenApiSchema>
                    {
                        {"X", new OpenApiSchema {Type = "int", Format = "int32", Extensions = new Dictionary<string, IOpenApiExtension> {{Markers.ValueObjectFormatMarker, null}}}},
                        {"int", new OpenApiSchema {Type = "number", Format = "int32"}}
                    }
                },
                Paths = new OpenApiPaths
                {
                    {"/test", new OpenApiPathItem
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            { OperationType.Get , new OpenApiOperation
                            {
                                Parameters = new List<OpenApiParameter>
                                {
                                    new () {Name = "id2", Schema = new OpenApiSchema {Reference = new OpenApiReference {Id = "X"}}, Extensions = new Dictionary<string, IOpenApiExtension> {{Markers.ValueObjectFormatMarker, null}}}
                                }
                            }}
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new() { Name = "id", Schema = new OpenApiSchema {Reference = new OpenApiReference {Id = "X"}}, Extensions = new Dictionary<string, IOpenApiExtension> {{Markers.ValueObjectFormatMarker, null}}},
                            new() { Name = "test" }
                        }
                    }}
                }
            };
            var schemaGenerator = new SchemaGenerator(new(), new JsonSerializerDataContractResolver(new()));
            var schemaRepository = new SchemaRepository();
            var docFilterContext = new DocumentFilterContext(Array.Empty<ApiDescription>(), schemaGenerator, schemaRepository);
            _documentFilter.Apply(openApiDoc, docFilterContext);
            openApiDoc.Components.Schemas.Should().HaveCount(1);
            openApiDoc.Components.Schemas.Should().Contain(x => x.Key == "int");
            openApiDoc.Paths.Should().HaveCount(1);
            var path = openApiDoc.Paths.Single().Value;
            path.Parameters.Should().HaveCount(2);
            path.Parameters.Should().Contain(x => x.Name == "id" && x.Schema.Type == "int");
            var operation = path.Operations.Single().Value;
            operation.Parameters.Should().HaveCount(1);
            operation.Parameters.Single().Schema.Type.Should().Be("int");
        }
    }
}
