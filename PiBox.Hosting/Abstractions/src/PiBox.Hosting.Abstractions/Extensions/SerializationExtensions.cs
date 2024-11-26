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
        static SerializationExtensions()
        {
            var kindConverters = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => x.IsInterface && x.IsAssignableTo(typeof(IKindSpecifier)) && x != typeof(IKindSpecifier))
                .Select(x => typeof(KindSpecifierConverter<>).MakeGenericType(x))
                .Select(Activator.CreateInstance)
                .OfType<JsonConverter>();
            foreach (var converter in kindConverters)
                DefaultOptions.Converters.Add(converter);
        }

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

    }

    internal class ValueObjectYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type.GetCustomAttributes().Any(attr => attr.GetType() == typeof(ValueObjectAttribute)
                                                          || (attr.GetType().IsGenericType &&
                                                              attr.GetType().GetGenericTypeDefinition() == typeof(ValueObjectAttribute<>)));
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var scalar = parser.Consume<Scalar>();
            var valueType = type.GetProperty("Value")!.PropertyType;
            return TypeDescriptor.GetConverter(valueType).ConvertFromInvariantString(scalar.Value);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var val = value!.GetType().GetProperty("Value")!.GetGetMethod()!
                .Invoke(value, [])!
                .ToString()!;
            emitter.Emit(new Scalar(null, null, val, ScalarStyle.Plain, true, false));
        }
    }

    internal class KindSpecifierConverter<T> : JsonConverter<T> where T : IKindSpecifier
    {
        private readonly IDictionary<string, Type> _types = typeof(T).Assembly
            .GetTypes().Where(x => x is { IsClass: true, IsAbstract: false } && x.GetInterface(typeof(T).Name) != null)
            .ToDictionary(x => ((T)Activator.CreateInstance(x)!).Kind.ToLowerInvariant(), x => x);
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var typeName = FindProperty(root, nameof(IKindSpecifier.Kind)).GetString()!.ToLowerInvariant();
            if (!_types.TryGetValue(typeName, out var type))
                throw new JsonException($"Unknown Type: {typeName}");

            var json = root.GetRawText();
            return (T)JsonSerializer.Deserialize(json, type, options)!;
        }

        private static JsonElement FindProperty(JsonElement element, string propertyName)
        {
            foreach (var property in element.EnumerateObject().Where(property => string.Equals(property.Name, propertyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return property.Value;
            }
            throw new Exception("Could not find property: " + propertyName);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var newOptions = new JsonSerializerOptions(options);
            var converters = newOptions.Converters.Where(x => x.GetType().IsGenericType
                                                              && x.GetType().GetGenericTypeDefinition() == typeof(KindSpecifierConverter<>)).ToList();
            foreach (var converter in converters)
                newOptions.Converters.Remove(converter);
            var json = JsonSerializer.Serialize(value, newOptions);
            writer.WriteRawValue(json);
        }
    }

    public enum SerializationMethod
    {
        Json = 0,
        Yaml = 1
    }

    public interface IKindSpecifier
    {
        string Kind { get; }
    }
}
