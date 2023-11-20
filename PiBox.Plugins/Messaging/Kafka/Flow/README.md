# Kafka Flow Plugin

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

This plugin provides the nuget packages from [KafkaFlow](https://github.com/Farfetch/kafkaflow) as Pibox plugin.

The containers that you need are also provided in the docker-compose.yaml file. You just need to configure the consumer(
s) and the producer(s) and use them accordingly.

## Installation

Install the Plugin via Nuget

```
dotnet add package PiBox.Plugins.Messaging.Kafka.Flow
```

or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Messaging.Kafka.Flow" Version=""/>
```

## Appsettings.yml

Configure your `appsettings.yml` accordingly.

```yaml
kafka:
  client:
    bootstrapServers: "localhost:9092,localhost:9093"
  #    securityProtocol: "SaslPlaintext"
  #    saslPassword: "asdf"
  #    saslUsername: "asdf"
  #    saslMechanism: "Plain"
  #    sslCaLocation: ""
  #    enableSslCertificateVerification: "false"
  schemaRegistry:
    url: "localhost:8081"
#    basicAuthUserInfo: "developer:SECRET"
#    enableSslCertificateVerification: "false"
```

## Containers

The docker-compose.yaml will run the following containers:

* zookeeper
* broker
* schema-registry
* control-center

To run all containers

```shell
docker-compose up
```

To stop and remove the currently running containers

```shell
docker-compose down
```

## Usage

### Plugin configuration

```c#
public class KafkaFlowExamplePlugin : IPluginServiceConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger? _logger;

        public KafkaFlowExamplePlugin(IConfiguration configuration, ILogger<KafkaFlowExamplePlugin>? logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        //Configure your consumers & producers
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.ConfigureKafka(_configuration, _logger, kafkaFlowBuilder => kafkaFlowBuilder
                //possible configurations:

                // producerConfig is none
                // producer is added to a Dictionary<Type, Action<IProducerConfigurationBuilder>>
                // type is typeof(TMessage)
                .AddTypedProducer<TMessage>("protobuf-topic")

                // producer is added to a Dictionary<Type, Action<IProducerConfigurationBuilder>>
                // type is typeof(TMessage)
                .AddTypedProducer<TMessage>("protobuf-topic", producerConfig)

                // producerConfig is none
                // producer is added to a List<(string, Action<IProducerConfigurationBuilder>)
                // the string added is typeof(TProducer).Name
                .AddProducer<TProducer>("protobuf-topic")

                // producer is added to a List<(string, Action<IProducerConfigurationBuilder>)
                // the string added is typeof(TProducer).Name
                .AddProducer<TProducer>("protobuf-topic", producerConfig)

                // consumer is added to a List<Action<IConsumerConfigurationBuilder>>
                .AddConsumer<TMessageHandler>("protobuf-topic", "mygroup"));

                // consumer is added to a List<Action<IConsumerConfigurationBuilder>>
                // dead letter message is produced on dead letter topic in case of unsuccessful processing of the message
                .AddConsumerWithDeadLetter<TMessageHandler, TMessage, TDeadLetterMessage>("protobuf-topic", "dead-letter-topic", "mygroup")
        }
    }
```

[Click here](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.Extensions.DependencyInjection.IServiceCollection?view=dotnet-plat-ext-6.0)
to read more about the IServiceCollection interface.

#### Protobuf message format example

```protobuf
syntax = "proto3";
message TMessage {
  string Message = 1;
  int32 Code = 2;
}
```
[Click here](https://developers.google.com/protocol-buffers) to read more about protobuf

### Consumer usage

```c#
public class ProtobufMessageHandler : IMessageHandler<ProtobufLogMessage>
{
    public Task Handle(IMessageContext context, ProtobufLogMessage message)
    {
        // Do something
    }
}

```

### Consumer with dead letter message producer usage

```c#
// The DltMessageHandler inherits from IMessageHandler<TMessage> (used to create a message handler)
// It also produces a dead letter message if there was an exception in ProcessMessageAsync
public class ProtobufDltMessageHandler : DltMessageHandler<TMessage, TDeadLetterMessage>
    {
        protected override async Task ProcessMessageAsync(IMessageContext context, TMessage message)
        {
            // Do something
        }

        protected override TDeadLetterMessage HandleError(IMessageContext context, TMessage message, Error error)
        {
            // Do something
        }
    }
```

### Producer usage (optional)

```c#
public class SampleProducer
    {
        private readonly IMessageProducer _producer;

        public SampleProducer(IProducerAccessor producerAccessor)
        {
            _producer = producerAccessor.GetProducer("TProducer");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _producer.ProduceAsync("protobuf-topic", messageKey, messageValue);
        }
    }
```
