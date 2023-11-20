using System.ComponentModel;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using PiBox.Api.OpenApi.Filters.Schema;
using Swashbuckle.AspNetCore.SwaggerGen;
// ReSharper disable UnusedMember.Local

namespace PiBox.Api.OpenApi.Tests.Filters.Schema
{
    public class ReadOnlyPropertyFilterTests
    {
        private readonly ISchemaFilter _filter = new ReadOnlyPropertySchemaFilter();

        [Test]
        public void SetsOpenApiSchemaToReadOnly()
        {
            var openApiSchema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    {"Id", new OpenApiSchema {ReadOnly = false}},
                    {"Name", new OpenApiSchema {ReadOnly = false}}
                }
            };
            var schemaContext = new SchemaFilterContext(typeof(SampleObj), null, null);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Properties["Id"].ReadOnly.Should().BeTrue();
            openApiSchema.Properties["Name"].ReadOnly.Should().BeFalse();
        }

        private class SampleObj
        {
            [ReadOnly(true)]
            public Guid Id { get; set; }

            public string Name { get; set; } = null!;
        }
    }
}
