using Confluent.SchemaRegistry;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Testing.Assertions;
using PiBox.Testing.Extensions;
using UnitTests;
using UnitTests2;
using FileOptions = Google.Protobuf.Reflection.FileOptions;

namespace PiBox.Plugins.Messaging.Kafka.Flow.Tests
{
    public class ProtobufTypeNameResolverTests
    {
        private ISchemaRegistryClient _schemaRegistryClient = null!;
        private ILogger _logger = null!;
        private ProtobufTypeNameResolver _protobufTypeNameResolver = null!;

        [SetUp]
        public void Setup()
        {
            _schemaRegistryClient = Substitute.For<ISchemaRegistryClient>();
            _logger = new FakeLogger<ProtobufTypeNameResolver>();
            _protobufTypeNameResolver = new ProtobufTypeNameResolver(_schemaRegistryClient, _logger);
        }

        private static Schema CreateSchema(string name, string package = "UnitTests", string csharpNamespace = null)
        {
            var fileDescriptorProto = new FileDescriptorProto { Package = package, Name = "default" };
            var messageType = new DescriptorProto
            {
                Name = name,
                Field =
                {
                    new FieldDescriptorProto
                    {
                        Name = "Message",
                        Type = FieldDescriptorProto.Types.Type.String
                    }
                },
                Options =
                {

                }
            };
            if (csharpNamespace is not null)
                fileDescriptorProto.Options = new FileOptions { CsharpNamespace = csharpNamespace };
            fileDescriptorProto.MessageType.Add(messageType);
            return new Schema(name, SchemaType.Protobuf)
            {
                References = new List<SchemaReference>(),
                SchemaString = fileDescriptorProto.ToByteString().ToBase64(),
                SchemaType = SchemaType.Protobuf
            };
        }

        [Test]
        public void ProtobufTypeNameResolverResolvesMessageTypes()
        {
            var messageTypes = _protobufTypeNameResolver.GetInaccessibleValue<IDictionary<string, string>>(@"_messageTypes");
            messageTypes.Should().NotBeNull();
            messageTypes.Should().ContainKey(typeof(MessageWithoutOptions).FullName!.ToLowerInvariant());
            messageTypes.Should().ContainKey(typeof(MessageWithCSharpOption).FullName!.ToLowerInvariant());
        }

        [Test]
        public void ProtobufTypeNameResolverFindsTypeWithoutCSharpOption()
        {
            var schema = CreateSchema(nameof(MessageWithoutOptions));
            _schemaRegistryClient.GetSchemaAsync(Arg.Any<int>(), Arg.Any<string>())
                .Returns(Task.FromResult(schema));
            var typeName = _protobufTypeNameResolver.Resolve(0);
            typeName.Should().NotBeNull();
            typeName.Should().Be(typeof(MessageWithoutOptions).FullName);
        }

        [Test]
        public void ProtobufTypeNameResolverFindsTypeWithCSharpOption()
        {
            var schema = CreateSchema(nameof(MessageWithCSharpOption), package: "SomeStrangeBehaviour", csharpNamespace: "UnitTests2");
            _schemaRegistryClient.GetSchemaAsync(Arg.Any<int>(), Arg.Any<string>())
                .Returns(Task.FromResult(schema));
            var typeName = _protobufTypeNameResolver.Resolve(0);
            typeName.Should().NotBeNull();
            typeName.Should().Be(typeof(MessageWithCSharpOption).FullName);
        }

        [Test]
        public void ProtobufTypeNameResolverReturnsNullOnNotExistingTypes()
        {
            var schema = CreateSchema("NothingName", package: "NothingPackage");
            _schemaRegistryClient.GetSchemaAsync(Arg.Any<int>(), Arg.Any<string>())
                .Returns(Task.FromResult(schema));
            var typeName = _protobufTypeNameResolver.Resolve(1);
            typeName.Should().NotBeNull();
            typeName.Should().Be("NothingPackage.NothingName");
        }

    }
}
