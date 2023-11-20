# PiBox.Plugins.Management.Unleash
[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Management.Unleash is a `plugin` that allows ` to use feature flags` to enable `CI/CD with control when and how are features enabled `.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Management.Unleash
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Management.Unleash" Version="" />
```

This plugin uses
* https://github.com/microsoft/FeatureManagement-Dotnet (feature flags abstraction)
* https://github.com/Unleash/unleash-client-dotnet (client sdk)
* https://github.com/Unleash/unleash (server)

## Configuration
```yaml
unleash:
  appName: "my-fancy-app"
  apiUri: "http://localhost:4242/api/"
  apiToken: "[create-in-local-instance-web-ui]"
  projectId: "default" //in free/opensource mode there is just one project, always the same
  instanceTag: "my-fancy-backup2"
  environment: "development"
```
## Usage

### Feature flag checks
```csharp

public class ExampleService
{
    private readonly IFeatureManager _featureManager;

    public ExampleService(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public void CheckFeatureFlag()
    {
         var isEnabled= await featureManager.IsEnabledAsync("myFeatureFlag");
    }

    public void CheckFeatureFlagWithActivatedContextualUserIdFilter()
    {
          var isEnabled = await featureManager.IsEnabledAsync("hello", new UserIdContext(){UserId = "id1"});
    }
}
```
see also https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core6x#feature-flag-checks
### Controller & Actions
```csharp
    [ApiController, Route("test")]
    [FeatureGate("my-feature-flag-for-a-whole-controller")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [FeatureGate("my-feature-flag-for-get-action")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [FeatureGate("my-feature-flag-for-post-action")]
        public IActionResult Create()
        {
            return View();
        }
    }
```
see also https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core6x#controller-actions

### MVC Views
```csharp
@addTagHelper *, Microsoft.FeatureManagement.AspNetCore
```
```csharp
<feature name="FeatureA">
    <p>This can only be seen if 'FeatureA' is enabled.</p>
</feature>
```

see also https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core6x#controller-actions

### MVC Filters
```csharp
using Microsoft.FeatureManagement.FeatureFilters;

IConfiguration Configuration { get; set;}

public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc(options => {
        options.Filters.AddForFeature<ThirdPartyActionFilter>(MyFeatureFlags.FeatureA);
    });
}
```
see also https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core6x#controller-actions

### Middleware
```csharp
app.UseMiddlewareForFeature<ThirdPartyMiddleware>(MyFeatureFlags.FeatureA);
```
see also https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core6x#middleware
