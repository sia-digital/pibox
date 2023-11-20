using FluentAssertions;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using PiBox.Api.OpenApi.Filters.Common;
using PiBox.Api.OpenApi.Filters.Schema;
using Swashbuckle.AspNetCore.SwaggerGen;
using Vogen;

namespace PiBox.Api.OpenApi.Tests.Filters.Schema
{
    public class ValueObjectFilterTests
    {
        private readonly ISchemaFilter _filter = new ValueObjectSchemaFilter(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml"));

        [Test]
        public void SpecifiesTheRemoveFlagForValueObjects()
        {
            var openApiSchema = new OpenApiSchema();
            var schemaGenerator = new SchemaGenerator(new(), new JsonSerializerDataContractResolver(new()));
            var schemaRepository = new SchemaRepository();
            var schemaContext = new SchemaFilterContext(typeof(ValueObjectDefaultIsInt), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectString), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectBoolean), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectShort), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectLong), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectByte), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectFloat), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectDecimal), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectDouble), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectDateTime), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectDateOnly), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectTimeOnly), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectDateTimeOffset), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
            openApiSchema = new OpenApiSchema();

            schemaContext = new(typeof(ValueObjectGuid), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Extensions.Should().ContainKey(Markers.ValueObjectFormatMarker);
        }

        [Test]
        public void DontApplyFilterWithNonValueObjects()
        {
            var openApiSchema = new OpenApiSchema();
            var schemaGenerator = new SchemaGenerator(new(), new JsonSerializerDataContractResolver(new()));
            var schemaRepository = new SchemaRepository();
            var schemaContext = new SchemaFilterContext(typeof(int), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(string), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(bool), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(short), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(long), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(byte), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(float), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(decimal), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(double), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(DateTime), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(DateOnly), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(TimeOnly), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(DateTimeOffset), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();

            schemaContext = new(typeof(Guid), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().BeNull();
        }

        [Test]
        public void OpenApiSchemaGetsOnlyChangedForValueObjects()
        {
            var openApiSchema = new OpenApiSchema { Type = "object" };
            var schemaContext = new SchemaFilterContext(typeof(X), null, null);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().Be("object");
        }

        [Test]
        public void OpenApiSchemaGetsOnlyChangedForValueObjectsWithinAClass()
        {
            var openApiSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
            {
                {"id", new OpenApiSchema {Type = "object" }},
                {"id2", new OpenApiSchema {Type = "object" }},
                {"ids", new OpenApiSchema {Type = "array", Items = new OpenApiSchema()}}
            }
            };
            var schemaGenerator = new SchemaGenerator(new(), new JsonSerializerDataContractResolver(new()));
            var schemaRepository = new SchemaRepository();
            var schemaContext = new SchemaFilterContext(typeof(Y), schemaGenerator, schemaRepository);
            _filter.Apply(openApiSchema, schemaContext);
            openApiSchema.Type.Should().Be("object");
            openApiSchema.Properties["id"].Type.Should().Be("integer");
            openApiSchema.Properties["id"].Format.Should().Be("int32");

            openApiSchema.Properties["id2"].Type.Should().Be("integer");
            openApiSchema.Properties["id2"].Format.Should().Be("int32");
            openApiSchema.Properties["id2"].Description.Should().Be("Test summary");

            openApiSchema.Properties["ids"].Type.Should().Be("array");
            openApiSchema.Properties["ids"].Items.Type.Should().Be("integer");
            openApiSchema.Properties["ids"].Items.Format.Should().Be("int32");
            openApiSchema.Properties["ids"].Items.Description.Should().Be("list of ids");
        }
    }

    public class X { }

    public class Y
    {
        public ValueObjectDefaultIsInt Id { get; set; }
        /// <summary>
        /// Test summary
        /// </summary>
        public ValueObjectDefaultIsInt Id2 { get; set; }

        /// <summary>
        /// list of ids
        /// </summary>
        public IList<ValueObjectDefaultIsInt> Ids { get; set; }
    }

    [ValueObject]
    public partial struct ValueObjectDefaultIsInt
    {
        public static Validation Validate(int value) => Validation.Ok;
    }

    [ValueObject(typeof(string))]
    public partial struct ValueObjectString
    {
        public static Validation Validate(string value) => Validation.Ok;
    }

    [ValueObject(typeof(bool))]
    public partial struct ValueObjectBoolean
    {
        public static Validation Validate(bool value) => Validation.Ok;
    }

    [ValueObject(typeof(short))]
    public partial struct ValueObjectShort
    {
        public static Validation Validate(short value) => Validation.Ok;
    }

    [ValueObject(typeof(long))]
    public partial struct ValueObjectLong
    {
        public static Validation Validate(long value) => Validation.Ok;
    }

    [ValueObject(typeof(byte))]
    public partial struct ValueObjectByte
    {
        public static Validation Validate(byte value) => Validation.Ok;
    }

    [ValueObject(typeof(float))]
    public partial struct ValueObjectFloat
    {
        public static Validation Validate(float value) => Validation.Ok;
    }

    [ValueObject(typeof(decimal))]
    public partial struct ValueObjectDecimal
    {
        public static Validation Validate(decimal value) => Validation.Ok;
    }

    [ValueObject(typeof(double))]
    public partial struct ValueObjectDouble
    {
        public static Validation Validate(double value) => Validation.Ok;
    }

    [ValueObject(typeof(DateTime))]
    public partial struct ValueObjectDateTime
    {
        public static Validation Validate(DateTime value) => Validation.Ok;
    }

    [ValueObject(typeof(DateOnly))]
    public partial struct ValueObjectDateOnly
    {
        public static Validation Validate(DateOnly value) => Validation.Ok;
    }

    [ValueObject(typeof(TimeOnly))]
    public partial struct ValueObjectTimeOnly
    {
        public static Validation Validate(TimeOnly value) => Validation.Ok;
    }

    [ValueObject(typeof(DateTimeOffset))]
    public partial struct ValueObjectDateTimeOffset
    {
        public static Validation Validate(DateTimeOffset value) => Validation.Ok;
    }

    [ValueObject(typeof(Guid))]
    public partial struct ValueObjectGuid
    {
        public static Validation Validate(Guid value) => Validation.Ok;
    }
}
