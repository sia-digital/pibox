# PiBox.Hosting.Generator

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Hosting.Generator is a `dotnet source generator package` that allows `dotnet dev's` to `use the pibox web host to discover available pibox plugins`.

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

## Concept

The source generator will scan all assemblies which are referenced by the PiBox.Hosting.Webhost project and add any classes or interfaces which are implementing

* the interface ```IPluginActivateable```
* any class attribute which starts with the name ```PiBox```

to generate a class like the following example

```c#
using System;
using System.Diagnostics.CodeAnalysis;
using System.CodeDom.Compiler;
namespace PiBox.Generated;

[ExcludeFromCodeCoverage]
[GeneratedCode("PiBox.Hosting.Generator", "0.5.0.0")]
public class PiBoxPluginTypes {
 public static Type[] All = new Type[] {
  typeof(PiBox.Hosting.WebHost.Sample.HangfireTestJob),
  typeof(PiBox.Hosting.WebHost.Sample.WebHostSamplePlugin),
  typeof(PiBox.Hosting.WebHost.Sample.NpgsqlDbConfiguration),
  typeof(PiBox.Api.OpenApi.OpenApiConfiguration),
  typeof(PiBox.Api.OpenApi.OpenApiPlugin),
  typeof(PiBox.Plugins.Authorization.Keycloak.KeycloakPlugin),
  typeof(PiBox.Plugins.Authorization.Keycloak.KeycloakPluginConfiguration),
  typeof(PiBox.Plugins.Endpoints.RestResourceEntity.RestResourceEntityPlugin),
  typeof(PiBox.Plugins.Jobs.Hangfire.HangfireConfiguration),
  typeof(PiBox.Plugins.Jobs.Hangfire.HangFirePlugin),
  typeof(PiBox.Plugins.Persistence.EntityFramework.EntityFrameworkPlugin),
  typeof(PiBox.Hosting.Abstractions.Middlewares.ExceptionMiddleware),
  typeof(PiBox.Hosting.Abstractions.Middlewares.RequestContentLengthLimitMiddleware),
  typeof(PiBox.Plugins.Handlers.Cqrs.SimpleResource.CqrsSimpleResourcePlugin),
 };
}
```

This class then gets referenced by the webhost's Program.cs e.g.

**If you don't include the generator package, you must specify the allowed plugin types manually!**

```csharp
using PiBox.Hosting.WebHost;
PluginWebHostBuilder.RunDefault(PiBox.Generated.PiBoxPluginTypes.All);
```

The webhost's service-, host- and application-configurators then access this list of types to activate and load all the found plugins on startup.

### Excludes

The following namespaces will be ignored when the source generator goes through all the assemblies at build time:

* Microsoft
* System
* FastEndpoints
* testhost
* netstandard
* Newtonsoft
* mscorlib
* NuGet
* NSwag
* FluentValidation
* YamlDotNet
* Accessibility
* NJsonSchema
* Namotion"

Changes to this list may happen in the future based on new requirements.
