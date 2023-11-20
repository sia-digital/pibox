using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using PiBox.Api.OpenApi.Filters.Operation;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedMember.Local

namespace PiBox.Api.OpenApi.Tests.Filters.Operation
{
    public class FormFileFilterTests
    {
        private readonly IOperationFilter _filter = new FormFileFilter();

        [Test]
        public void SetFormFilter()
        {
            var openApiSchema = new OpenApiOperation();
            var schemaGenerator = DefaultSchemaGenerator(out var schemaRepository, out var description);
            var schemaContext = new OperationFilterContext(description, schemaGenerator, schemaRepository,
                typeof(FormFileFilterTests).GetMethod(nameof(TestMethodWithFormFile),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static));

            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.RequestBody.Content.Should().NotBeEmpty();
            openApiSchema.RequestBody.Content["multipart/form-data"].Should().BeOfType(typeof(OpenApiMediaType));
        }
        [Test]
        public void SetFormFilterWithoutFormFile()
        {
            var openApiSchema = new OpenApiOperation();
            var schemaGenerator = DefaultSchemaGenerator(out var schemaRepository, out var description);
            var schemaContext = new OperationFilterContext(description, schemaGenerator, schemaRepository,
                typeof(FormFileFilterTests).GetMethod(nameof(TestMethodWithout),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static));

            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.RequestBody.Should().BeNull();

        }
        private static SchemaGenerator DefaultSchemaGenerator(out SchemaRepository schemaRepository, out ApiDescription description)
        {
            var schemaGenerator = new SchemaGenerator(new SchemaGeneratorOptions(),
                new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
            schemaRepository = new SchemaRepository();
            description = new ApiDescription() { ActionDescriptor = new ActionDescriptor() };
            return schemaGenerator;
        }
        private static void TestMethodWithFormFile([FromBody] IFormFile testFile)
        {
            if (testFile == null) throw new ArgumentNullException(nameof(testFile));
        }

        private static void TestMethodWithout([FromBody] string testFile)
        {
            if (testFile == null) throw new ArgumentNullException(nameof(testFile));
        }
    }
}
