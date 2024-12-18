using System.Collections.Concurrent;
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
            Converters = { new JsonStringEnumConverter(), new KindSpecifierConverterFactory() },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreReadOnlyFields = true,
            WriteIndented = false
        };

        internal static DeserializerBuilder GetYamlDeserializerBuilder() => new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithDuplicateKeyChecking()
            .WithTypeConverter(new ValueObjectYamlTypeConverter())
            .WithTypeConverter(new KindSpecifierYamlTypeConverter());

        private static readonly IDeserializer _deserializer = GetYamlDeserializerBuilder().Build();

        internal static SerializerBuilder GetYamlSerializerBuilder() => new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new ValueObjectYamlTypeConverter())
            .WithTypeConverter(new KindSpecifierYamlTypeConverter());

        private static readonly ISerializer _serializer = GetYamlSerializerBuilder().Build();

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

    internal class KindSpecifierYamlTypeConverter : IYamlTypeConverter
    {
        private static readonly IDeserializer _deserializer = SerializationExtensions.GetYamlDeserializerBuilder()
            .WithoutTypeConverter(typeof(KindSpecifierYamlTypeConverter)).Build();
        private static readonly ISerializer _serializer = SerializationExtensions.GetYamlSerializerBuilder()
            .WithoutTypeConverter(typeof(KindSpecifierYamlTypeConverter)).Build();
        private readonly IDictionary<Type, Type[]> _kindTypes = new ConcurrentDictionary<Type, Type[]>();
        private readonly IDictionary<string, Type> _kindNames = new ConcurrentDictionary<string, Type>();

        public bool Accepts(Type type)
        {
            return typeof(IKindSpecifier).IsAssignableFrom(type);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (!_kindTypes.TryGetValue(type, out var possibleTypes))
            {
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes()).ToArray();
                possibleTypes = types.Where(x => x.IsClass && type.IsAssignableFrom(x)).ToArray();
                _kindTypes[type] = possibleTypes;
                foreach (var pt in possibleTypes)
                {
                    var kindName = ((IKindSpecifier)Activator.CreateInstance(pt)!).Kind.ToLowerInvariant();
                    _kindNames[kindName] = pt;
                }
            }

            var yamlValue = parser.Consume<Scalar>().Value;
            var kindNode = _deserializer.Deserialize<Dictionary<string, object>>(yamlValue)
                .FirstOrDefault(x => x.Key.Equals(nameof(IKindSpecifier.Kind), StringComparison.InvariantCultureIgnoreCase))
                .Value as string;
            if (string.IsNullOrEmpty(kindNode) || !_kindNames.TryGetValue(kindNode, out var kindType))
                throw new YamlException($"Unknown type for kind: {kindNode ?? "not set"}");

            return _deserializer.Deserialize(yamlValue, kindType);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var yamlText = _serializer.Serialize(value);
            emitter.Emit(new Scalar(null, null, yamlText, ScalarStyle.Plain, true, false));
        }
    }

    internal class KindSpecifierConverterFactory : JsonConverterFactory
    {
        private readonly IDictionary<Type, JsonConverter> _converters = new ConcurrentDictionary<Type, JsonConverter>();
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IKindSpecifier).IsAssignableFrom(typeToConvert) && typeToConvert.IsInterface;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (_converters.TryGetValue(typeToConvert, out var cachedConverter))
                return cachedConverter;
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes());
            var converterType = typeof(KindSpecifierConverter<>).MakeGenericType(typeToConvert);
            var converter = (JsonConverter)Activator.CreateInstance(converterType, [types])!;
            _converters.Add(typeToConvert, converter);
            return converter;
        }

        private class KindSpecifierConverter<T> : JsonConverter<T> where T : IKindSpecifier
        {
            private readonly IDictionary<string, Type> _types;

            public KindSpecifierConverter(IEnumerable<Type> types)
            {
                _types = types
                    .Where(x => x is { IsClass: true, IsAbstract: false } && x.GetInterface(typeof(T).Name) != null)
                    .ToDictionary(x => ((T)Activator.CreateInstance(x)!).Kind.ToLowerInvariant(), x => x);
            }
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
                var converter = newOptions.Converters.Single(x => x.GetType() == typeof(KindSpecifierConverterFactory));
                newOptions.Converters.Remove(converter);
                var json = JsonSerializer.Serialize(value, newOptions);
                writer.WriteRawValue(json);
            }
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
