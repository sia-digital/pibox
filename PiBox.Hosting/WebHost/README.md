# PiBox.Hosting.Webhost

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Hosting.WebHost is the `core package` that allows `dotnet dev's` to `use the pibox web host and pibox plugins`.

**This package is mandatory, without it you can't use any plugins**

## Installation

To install the nuget package follow these steps:

```shell

dotnet add package PiBox.Hosting.Generator # this is required so the web host can discover & load the plugins
dotnet add package PiBox.Hosting.WebHost
```

or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Hosting.Generator" Version="" />
<PackageReference Include="PiBox.Hosting.WebHost" Version="" />
```

Rewrite the Entrypoint Program.cs to have following code:

```csharp
using PiBox.Hosting.WebHost;
PluginWebHostBuilder.RunDefault(PiBox.Generated.PiBoxPluginTypes.All);
```

## Config files and ENV vars

PiBox will try to load config files and values in the following order

1. `appsettings*.json`
2. `appsettings*.yaml`
3. `appsettings*.yml`
4. `appsettings.json`
5. `appsettings.yaml`
6. `appsettings.yml`
7. ENV variables

File names must always start with `appsettings` or they will be ignored!

Nested settings use `_` to build their hierarchy as ENV variables

```yaml
host:
  urls: http://+:5300 # optional, default is 8080;separate multiple urls with ","
```

becomes

```shell
HOST_URLS=http://+:5300 # optional, default is 8080;separate multiple urls with ","
```

## General settings

Configure your config file with these properties

```yaml
host:
  urls: http://+:5300 # optional, default is 8080;separate multiple urls with ","
  maxRequestSize: 8388608 # optional, default is 8 MB

```

or as ENV variables

```shell
HOST_URLS=http://+:5300 # optional, default is 8080;separate multiple urls with ","
HOST_MAXREQUESTSIZE=8388608 # optional, default is 8 MB
```

## Logging

Configure your appsettings.logging.yml with these properties

```yaml
serilog:
  minimumLevel: Debug # or Warn or Info
  override:
    System: Error # or Warning or Information
    Microsoft: Error # or Warning or Information

```

or as ENV variables

```shell
SERIOLOG_minimumLevel=Debug
SERIOLOG_OVERRIDE_SYSTEM=Error
SERIOLOG_OVERRIDE_MICROSOFT=Error
```

## Usage

Rewrite your entrypoint Program.cs to have following code:

```csharp
PiBox.Hosting.WebHostPluginWebHostBuilder.RunDefault(PiBox.Generated.PiBoxPluginTypes.All);
```

Now you can add additional plugins as nuget packages to your project and configure them via config files/settings and/or in your web host plugin

## Metrics

Pibox uses open telemetry for it's metrics capabilities. Further info can be found here&#x20;

[#metrics](../Abstractions/#metrics "mention")

## Tracing settings

Pibox uses open telemetry for it's tracing capabilities. Further info can be found here&#x20;

{% embed url="https://opentelemetry.io/docs/instrumentation/net/getting-started/" %}

If there is a tracing host configured, the service will try to send any traces to this host. it will also enrich the log messages with SpanIds and TraceIds.

```yaml
tracing:
  host: http://localhost:3333
```
