# PiBox.Plugins.Jobs.Hangfire

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Jobs.Hangfire is a `plugin` that allows `PiBox based services` to `use the hangfire package to manage and schedule (cron) jobs`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Jobs.Hangfire
```

or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Jobs.Hangfire" Version="" />
```

## Appsettings.yml

Configure your appsettings.yml with these properties

For example

```yaml
hangfire:
  enableJobsByFeatureManagementConfig: false
  allowedDashboardHost: localhost # you need to set this configuration to be able to access the dashboard from the specified host

featureManagement: # we can conveniently can use the microsoft feature management system to enable jobs based on configuration
  hangfireTestJob: true # if you have enabled the 'enableJobsByFeatureManagementConfig: true' then you can configure here if your jobs should run on execution or not, useful for multiple environments etc.
```

HangfireConfiguration.cs

```csharp
[Configuration("hangfire")]
public class HangfireConfiguration
{
    public string AllowedDashboardHost { get; set; }
    public bool EnableJobsByFeatureManagementConfig { get; set; }
    public int? PollingIntervalInMs { get; set; }
    public int? WorkerCount { get; set; }
}
```

## Configuration in your plugin host

```csharp
public class SamplePluginHost : IPluginServiceConfiguration
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        // here you can register your recurring jobs or just annotate them with the [RecurringJob] attribute
        serviceCollection.ConfigureJobs((jobRegister, _) =>
            {
                jobRegister.RegisterParameterizedRecurringAsyncJob<HangfireParameterizedTestJob, int>(Cron.Daily(), 1); // every day at 0:00 UTC
                jobRegister.RegisterRecurringAsyncJob<HangfireTestJob>(Cron.Hourly(15)); // every hour to minute 15 UTC
            });
    }
}
```

### Configuring Job Storage for Hangfire

The Hangfire plugin does not automatically register a storage for jobs. This is done on the implementing service's side by using the HangfireConfigurator logic that allows configuring Hangfire with a custom Configurator class in the implementing project.

```csharp
public class TestHangfireConfigurator : IHangfireConfigurator
{
    public void Configure(IGlobalConfiguration config)
    {
        var connectionString = "testConnection";
        config.UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions
            {
                InvisibilityTimeout = TimeSpan.FromMinutes(180) // controls the timeout until a second job is started when an existing job
                                                                // is running for longer than the specified minutes
                                                                // option not necessary for sql server storage
            });
    }

    public void ConfigureServer(BackgroundJobServerOptions options) {}
}
```

This is then applied with the following logic in the HangfirePlugin:

```csharp
public class HangFirePlugin(HangfireConfiguration configuration, IImplementationResolver implementationResolver, IHangfireConfigurator[] configurators)
        : IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginHealthChecksConfiguration
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddFeatureManagement();
        serviceCollection.AddHangfire(conf =>
            {
                conf.UseSerializerSettings(new JsonSerializerSettings());
                conf.UseSimpleAssemblyNameTypeSerializer();
                configurators.ForEach(x => x.Configure(conf));
            }
        );
    }
}
```

## Usage

Please take a look at the official Hangfire documentation https://docs.hangfire.io/en/latest/ and best practises https://docs.hangfire.io/en/latest/best-practices.html

Every job does out of the box 10 retries (with increasing wait times) if not configured otherwise.

### Auto registration of jobs

You can also define recurring jobs as simple classes and annotate them with the \[ReccuringJob] attribute which then auto-registers the job for you at startup

```csharp
[RecurringJob("0 0 * * *")]
public class HangfireTestJob : AsyncJob
{
    public HangfireTestJob(ILogger<HangfireTestJob> logger) : base(logger)
    {
    }

    protected override Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("TEst");
        return Task.FromResult<object>(cancellationToken);
    }
}
```

### Define jobs

#### Without parameters

```csharp
public class HangfireTestJob : AsyncJob
{
    public HangfireTestJob(ILogger<HangfireTestJob> logger) : base(logger)
    {
    }

    protected override Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("TEst");
        return Task.FromResult<object>(cancellationToken);
    }
}
```

#### With parameters

Job input parameters and results get serialized to json and are displayed in the hangfire dashboard, be aware of the possibility of leaking sensitive information!

```csharp
public class MyResult
{
    public int JobParameter {get;set;}
}
public class HangfireParameterizedTestJob : ParameterizedAsyncJob<int>
{
    public HangfireParameterizedTestJob(ILogger<HangfireParameterizedTestJob> logger) : base(logger)
    {
    }

    protected override Task<MyResult> ExecuteJobAsync(int value, CancellationToken cancellationToken)
    {
        var result = new MyResult(){ JobParameter = value};
        Logger.LogInformation("Test job for {Value}", value);
        return result; //
    }
}
```

### Fire and forget jobs or enqueue dynamically

You can create new jobs from anywhere in your service, see also https://docs.hangfire.io/en/latest/background-methods/index.html

```csharp
BackgroundJob.Enqueue<IEmailSender>(x => x.Send("hangfire@example.com"));
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));
```

### Attributes

#### UniquePerQueueAttribute
If you want the job only to be executed as one instance at any given point in time use the
```csharp
[UniquePerQueueAttribute("high")]
```

This ensures that there is only one job of the same type/name
and method parameters in processing or queued at any given point

#### JobCleanupExpirationTimeAttribute

With this you can specify how many days the results of a job
should be kept until it gets deleted

```csharp
[JobCleanupExpirationTimeAttribute(14)]
```

### Filters

#### LogJobExecutionFilter

This filter logs the start and finish of an job execution.

#### EnabledByFeatureFilter

This filter works in conjunction with the [microsoft feature management system](https://github.com/microsoft/FeatureManagement-Dotnet).
If you would like to be able to enable or disable the execution of your
jobs based on configuration this is the right tool for it.

**Default Feature management with file based configuration**
```yaml
hangfire:
  enableJobsByFeatureManagementConfig: true

featureManagement:
  hangfireTestJob: true
  neverRunThisJob: false
```

This allows you to enable jobs based on configuration files.
If you have enabled the setting

```yaml
enableJobsByFeatureManagementConfig: true
```
then you can configure here, if your jobs should run
on execution or not, useful for multiple environments etc.

If your service supports hot reloading of configuration files,
you can enable/disable jobs at run time.

**Feature management with the [pibox unleash plugin](https://sia-digital.gitbook.io/pibox/plugins/management/unleash)**

This works in conjunction with the plugin PiBox.Plugins.Management.Unleash.
This replaces the ability of setting the features via files.
Instead one can use the unleash api/service
and use feature flags for enabling the jobs.
Just make sure that the name of the job matches the name of the
feature flag you are creating in unleash.

The pibox unleash plugin then should do the rest of the heavy lifting.

Since the attribute resolves the feature on before executing the job,
changes to the configuration can be done at runtime with a maximal delay
based on how often the pibox unleash plugin refreshes its cache.
You can find more information in the documentation of the
[pibox unleash plugin](https://sia-digital.gitbook.io/pibox/plugins/management/unleash).
