# PiBox.Plugins.Authorization.Keycloak

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Authorization.Keycloak is a `plugin` that allows to `use authentication polices with an keycloak server instance for authentication & authorization`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Authorization.Keycloak
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Authorization.Keycloak" Version="" />
```

## Appsettings.yml

Configure your appsettings.yml with these properties

```yaml
keycloak:
  enabled: true #whenever the whole plugin should be active or not
  host: host.example #keycloak host url
  insecure: false i used protocol https vs http
  realms:
    prefix: '/realms/' # url prefix which should be used before the realm
  policies: # policies which should be available within the service/application
    - name: READ
      roles:
        - API_PERMISSIONS_READ
    - name: WRITE
      roles:
        - API_PERMISSIONS_WRITE
```

PluginReadmeTemplateConfig.cs
```csharp
[Configuration("keycloak")]
    public class KeycloakPluginConfiguration
    {
        public bool Enabled { get; set; }
        public string? Host { get; set; }
        public bool Insecure { get; set; }
        public RealmsConfig Realms { get; set; } = new RealmsConfig();
        public IList<AuthPolicy> Policies { get; set; } = new List<AuthPolicy>();
    }
    public class RealmsConfig
    {
        public string Prefix { get; set; } = "/auth/realms/";
    }
```
## Usage

```csharp
public class PluginReadme : IPluginEndpointsConfiguration
{
   public void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder, IServiceProvider serviceProvider)
        {
               endpointRouteBuilder
                .AddSimpleRestResource<MyResourceClass>(serviceProvider, "my-resource")
                .ForGet(configuration => configuration.WithPolicies("READ")) // assign individual policies for each http verb
                .ForGetList(configuration => configuration.WithPolicies("READ"))
                .ForDelete(configuration => configuration.WithPolicies("WRITE"))
                .ForPost(configuration => configuration.WithPolicies("WRITE"))
                .ForPut(configuration => configuration.WithPolicies("WRITE"))
                .ForAll(configuration => configuration.WithPolicies("WRITE", "READ")); // or for all at once

            endpointRouteBuilder.MapGet("/hello", async () => "Hello World!").RequireAuthorization("READ");
        }
}
```
```csharp
public class ExampleController : ControllerBase
{
    [HttpGet]
    [Authorize("READ")]
    public IActionResult Hello() => Ok();
}
```
