# PiBox.Plugins.Persistence.InMemory

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Persistence.InMemory is a `plugin` that allows `other PiBox components`
to `use the persistance repository abstractions with an in memory implementation for the repositories`.

Primary use cases are

* Prototyping
* Testing

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Persistence.InMemory
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Persistence.InMemory" Version="" />
```

## Configuration in your plugin host

```csharp
public class PluginReadme : IPluginEndpointsConfiguration, IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginControllerConfiguration, IPluginHealthChecksConfiguration
{
    private readonly PluginReadmeTemplateConfig _pluginReadmeTemplateConfig;

    public PluginReadme(PluginReadmeTemplateConfig pluginReadmeTemplateConfig)
    {
        _pluginReadmeTemplateConfig = pluginReadmeTemplateConfig;
    }

    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        // the plugins does the following registrations
        serviceCollection.AddSingleton(typeof(InMemoryStore));
        serviceCollection.AddSingleton(typeof(IRepository<>), typeof(InMemoryRepository<>));
        serviceCollection.AddSingleton(typeof(IReadRepository<>), typeof(InMemoryRepository<>));
        // if you have other persistence plugins installted you'll
        // need to register the explitict types which should be used/registered with this plugin
        // otherwise the plugin load order will determine which persistence plugin will be used as a default for all
        // types from PiBox.Plugins.Persistence.Abstractions
    }
}
```

## Usage

You can use all of the registered types from the lib `PiBox.Plugins.Persistence.Abstractions`

```csharp
public interface IExampleService
{
    void DoStuff();
}

public class ExampleService : IExampleService
{
    private readonly PluginReadmeTemplateConfig _pluginReadmeTemplateConfig;
    private readonly IReadRepository<MyPoco> _readRepository;

    public ExampleService(PluginReadmeTemplateConfig pluginReadmeTemplateConfig, IReadRepository<MyPoco> readRepository)
    {
        _pluginReadmeTemplateConfig = pluginReadmeTemplateConfig;
        _readRepository = readRepository;
    }
    public async Task DoStuff()
    {
        // for example
        var result = await _readRepository.FindByIdAsync();
    }
}
```
