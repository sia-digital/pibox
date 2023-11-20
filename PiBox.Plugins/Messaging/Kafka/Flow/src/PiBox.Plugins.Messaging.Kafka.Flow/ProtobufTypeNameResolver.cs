using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.Serializer.SchemaRegistry;
using Microsoft.Extensions.Logging;

namespace PiBox.Plugins.Messaging.Kafka.Flow
{
    internal class ProtobufTypeNameResolver : ISchemaRegistryTypeNameResolver
    {
        private static readonly IDictionary<string, string> _messageTypes = GetMessageTypes();
        private readonly ISchemaRegistryClient _client;
        private readonly ILogger _logger;

        private static IDictionary<string, string> GetMessageTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => !string.IsNullOrEmpty(x.FullName)
                            && x.IsClass
                            && !x.IsAbstract
                            && x.GetInterfaces()
                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessage<>)))
                .ToDictionary(x => x.FullName!.ToLowerInvariant(), x => x.FullName!);
        }

        public ProtobufTypeNameResolver(ISchemaRegistryClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public string Resolve(int schemaId)
        {
            var schemaString = _client
                .GetSchemaAsync(schemaId, "serialized")
                .GetAwaiter()
                .GetResult().SchemaString;

            _logger.LogDebug("For {SchemaId} found {SchemaString}", schemaId, schemaString);

            var protoFields = FileDescriptorProto.Parser.ParseFrom(ByteString.FromBase64(schemaString));
            var typeName = protoFields.MessageType.FirstOrDefault()?.Name ?? "";
            var type = GetTypeFromFileDescriptorProto(protoFields, typeName);
            if (string.IsNullOrEmpty(type))
            {
                var fallbackTypeName = $"{protoFields.Package}.{typeName}";
                _logger.LogWarning("Could not find {MessageType} for {SchemaId} in proto fields {@ProtoFields}", fallbackTypeName, schemaId, protoFields);
                return fallbackTypeName;
            }

            _logger.LogDebug("For Schema {SchemaId} in Package {ProtobufPackage} with Name {ProtobufTypeName} resolved {TypeName}", schemaId, protoFields.Package, typeName, type);
            return type;
        }

        private static string GetTypeFromFileDescriptorProto(FileDescriptorProto proto, string typeName)
        {
            var type = ResolveTypeNameByNamespacePrefix(proto.Package, typeName);
            return type ?? ResolveTypeNameByNamespacePrefix(proto?.Options?.CsharpNamespace!, typeName);
        }

        private static string ResolveTypeNameByNamespacePrefix(string namespacePrefix, string typeName)
        {
            if (string.IsNullOrEmpty(namespacePrefix)) return null;
            var name = $"{namespacePrefix}.{typeName}".ToLower(CultureInfo.InvariantCulture);
            var hasValue = _messageTypes.TryGetValue(name, out var value);
            return hasValue ? value : null;
        }
    }

    // stolen from KafkaFlow only changed
    // new ConfluentProtobufTypeNameResolver() to
    // new CsharpNamespaceConfluentProtobufTypeNameResolver() and added the logger
    [ExcludeFromCodeCoverage]
    public static class ConsumerConfigurationBuilderExtensions
    {
        /// <summary>
        /// Registers a middleware to deserialize protobuf messages using schema registry which respects the options csharp_namespace
        /// </summary>
        /// <param name="middlewares">The middleware configuration builder</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public static IConsumerMiddlewareConfigurationBuilder AddSchemaRegistryProtobufSerializerWhichRespectsCsharpNamespaceDeclaration(
            this IConsumerMiddlewareConfigurationBuilder middlewares, ILogger logger)
        {
            return middlewares.Add(
                resolver => new SerializerConsumerMiddleware(
                    new ConfluentProtobufSerializer(resolver),
                    new SchemaRegistryTypeResolver(new ProtobufTypeNameResolver(resolver.Resolve<ISchemaRegistryClient>(), logger))));
        }
    }

    // stolen from KafkaFlow only changed
    // new ConfluentProtobufTypeNameResolver() to
    // new ProtobufTypeNameResolver() and added the logger
    [ExcludeFromCodeCoverage]
    public static class ProducerConfigurationBuilderExtensions
    {
        /// <summary>
        /// Registers a middleware to serialize protobuf messages using schema registry which respects the options csharp_namespace
        /// </summary>
        /// <param name="middlewares">The middleware configuration builder</param>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The json serializer configuration</param>
        /// <returns></returns>
        public static IProducerMiddlewareConfigurationBuilder AddSchemaRegistryProtobufSerializerWhichRespectsCsharpNamespaceDeclaration(
            this IProducerMiddlewareConfigurationBuilder middlewares, ILogger logger,
            ProtobufSerializerConfig config = null!)
        {
            return middlewares.Add(
                resolver => new SerializerProducerMiddleware(
                    new ConfluentProtobufSerializer(resolver, config),
                    new SchemaRegistryTypeResolver(new ProtobufTypeNameResolver(resolver.Resolve<ISchemaRegistryClient>(), logger))));
        }
    }
}
