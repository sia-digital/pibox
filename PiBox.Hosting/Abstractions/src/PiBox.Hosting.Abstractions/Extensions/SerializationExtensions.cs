using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vogen;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PiBox.Hosting.Abstractions.Extensions
{
    public static class SerializationExtensions
    {
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreReadOnlyFields = true,
            WriteIndented = false
        };

        private static readonly IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithDuplicateKeyChecking()
            .WithTypeConverter(new ValueObjectYamlTypeConverter())
            .Build();

        private static readonly ISerializer _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new ValueObjectYamlTypeConverter())
            .Build();

        public static string Serialize<T>(this T obj,
            SerializationMethod serializationMethod = SerializationMethod.Json)
        {
            return serializationMethod switch
            {
                SerializationMethod.Json => JsonSerializer.Serialize(obj, DefaultOptions),
                SerializationMethod.Yaml => _serializer.Serialize(obj),
                _ => throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null)
            };
        }

        public static T Deserialize<T>(this string content,
            SerializationMethod serializationMethod = SerializationMethod.Json)
        {
            return serializationMethod switch
            {
                SerializationMethod.Json => JsonSerializer.Deserialize<T>(content, DefaultOptions),
                SerializationMethod.Yaml => _deserializer.Deserialize<T>(content),
                _ => throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null)
            };
        }

        public static object Deserialize(this string content, Type targetType,
            SerializationMethod serializationMethod = SerializationMethod.Json)
        {
            return serializationMethod switch
            {
                SerializationMethod.Json => JsonSerializer.Deserialize(content, targetType, DefaultOptions),
                SerializationMethod.Yaml => _deserializer.Deserialize(content, targetType),
                _ => throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null)
            };
        }
        private class ValueObjectYamlTypeConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                return type.GetCustomAttributes().Any(attr => attr.GetType() == typeof(ValueObjectAttribute)
                                                              || (attr.GetType().IsGenericType &&
                                                                  attr.GetType().GetGenericTypeDefinition() == typeof(ValueObjectAttribute<>)));
            }

            public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
            {
                var scalar = parser.Consume<Scalar>();
                var valueType = type.GetProperty("Value")!.PropertyType;
                return TypeDescriptor.GetConverter(valueType).ConvertFromInvariantString(scalar.Value);
            }

            public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
            {
                var val = value!.GetType().GetProperty("Value")!.GetGetMethod()!
                    .Invoke(value, [])!
                    .ToString()!;
                emitter.Emit(new Scalar(null, null, val, ScalarStyle.Plain, true, false));
            }
        }
    }

    public enum SerializationMethod
    {
        Json = 0,
        Yaml = 1
    }
}
