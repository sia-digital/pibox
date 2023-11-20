# Kafka Flow Plugin Sample
This sample is a web-hosted application. It has two hosted services, each pushing a message to a kafka topic at an interval of 3 and 5 seconds respectively. A consumer outputs the message to the console. Another consumer deliberately returns an error while processing a message and produces a dead letter message.

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

### The KafkaFlowSamplePlugin class
* Adds **Kafka Flow** as a hosted service to a **[serviceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)** and configures it with a producer and two consumers
* Adds two more hosted services (**SampleProducer** and **SampleProducer2**), each producing a message every 3 and 5 seconds respectively

### The ProtobufMessageHandler class
* Consumes from the topic on which **SampleProducer** publishes
* Outputs the partition and offset of the **[context](https://github.com/Farfetch/kafkaflow/blob/7064e1590ead40d7ff925f5c77e8f1791ed149f8/src/KafkaFlow.Abstractions/IMessageContext.cs)** and the message to the console everytime a message comes in

### The ProtobufDltMessageHandler class
* Consumes from the topic on which **SampleProducer** publishes
* Returns an error while processing a message
* Publishes the error message on a dead letter topic

## The SampleProducer and SampleProducer2 classes
* Inherit from the class [BackgroundService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice) (Base class for implementing a long running IHostedService)
* Access the producer added in the **KafkaFlowSamplePlugin** by its name using [producerAccessor](https://github.com/Farfetch/kafkaflow/blob/7064e1590ead40d7ff925f5c77e8f1791ed149f8/src/KafkaFlow/Producers/IProducerAccessor.cs)
* Each produce a **message** every 3 and 5 seconds respectively, respective to the **[protobuf message format](https://developers.google.com/protocol-buffers/docs/proto3)** in **protobufLogMessage.proto**
