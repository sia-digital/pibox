using System.Collections;
using System.Reflection;
using System.Xml.XPath;
using Microsoft.OpenApi.Models;
using PiBox.Api.OpenApi.Filters.Common;
using PiBox.Hosting.Abstractions.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PiBox.Api.OpenApi.Filters.Schema
{
    public class ValueObjectSchemaFilter : ISchemaFilter
    {
        private readonly XPathNavigator[] _xmlDocNavigators;
        public ValueObjectSchemaFilter(IEnumerable<string> xmlDocuments)
        {
            _xmlDocNavigators = xmlDocuments.Select(x => new XPathDocument(x).CreateNavigator()).ToArray();
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(string)) return;
            var isValueObject = HasValueObjectAttribute(context.Type);
            if (isValueObject)
            {
                ApplyToValueObject(schema, context, context.Type);
                schema.Extensions.Add(Markers.ValueObjectFormatMarker, null);
            }
            else if (context.Type.IsClass)
                ApplyToClass(schema, context, context.Type);
        }

        private void ApplyToClass(OpenApiSchema schema, SchemaFilterContext context, Type type)
        {
            var properties = from prop in type.GetProperties()
                             where HasValueObjectAttribute(prop)
                                   || (prop.PropertyType.Implements<IEnumerable>()
                                       && prop.PropertyType.IsGenericType
                                       && HasValueObjectAttribute(prop.PropertyType.GetGenericArguments()[0]))
                             select prop;
            foreach (var prop in properties)
            {
                var key = schema.Properties.Keys.Single(x => string.Equals(x, prop.Name, StringComparison.InvariantCultureIgnoreCase));
                var containingType = prop.PropertyType.IsGenericType
                    ? prop.PropertyType.GetGenericArguments()[0]
                    : prop.PropertyType;
                ApplyToValueObject(schema.Properties[key], context, containingType, prop);
            }
        }

        private void ApplyToValueObject(OpenApiSchema schema, SchemaFilterContext context, Type type, PropertyInfo propertyInfo = null)
        {
            var underlyingType = GetUnderlyingType(type);
            var openApiSchema = context.SchemaGenerator.GenerateSchema(underlyingType, context.SchemaRepository);
            SetSchema(schema.Type == "array" ? schema.Items : schema, openApiSchema, propertyInfo);
        }

        private void SetSchema(OpenApiSchema source, OpenApiSchema toOverride, PropertyInfo propertyInfo = null)
        {
            source.Type = toOverride.Type;
            source.Format = toOverride.Format;
            source.AdditionalPropertiesAllowed = true;
            source.AdditionalProperties = null;
            source.Properties = null;
            source.Reference = null;
            source.Description = propertyInfo == null ? source.Description : GetDescription(propertyInfo);
        }

        private string GetDescription(MemberInfo memberInfo)
        {
            var memberName = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(memberInfo);
            var description = _xmlDocNavigators
                .Select(x => x.SelectSingleNode($"/doc/members/member[@name='{memberName}']/summary"))
                .FirstOrDefault(x => x != null);
            return description == null ? null : XmlCommentsTextHelper.Humanize(description.InnerXml);
        }

        private static bool HasValueObjectAttribute(MemberInfo type)
        {
            return GetRealType(type).GetCustomAttributes().FirstOrDefault(
                attr => attr.ToString() != null && attr.ToString()!.Contains("Vogen.ValueObject")) != null;
        }

        private static Type GetRealType(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propInfo)
                memberInfo = propInfo.PropertyType;
            return memberInfo as Type;
        }

        private static Type GetUnderlyingType(Type type)
        {
            return GetRealType(type).GetProperty("Value")!.PropertyType;
        }
    }
}
