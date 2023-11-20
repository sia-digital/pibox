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
  Host: localhost
  Port: 5432
  Database: postgres
  User: postgres
  Password: postgres
  InMemory: true
  DashboardUser: awesome-user #if you don't set this, you can't access the hangfire dashboard
  DashboardPassword: awesome-pw #if you don't set this, you can't access the hangfire dashboard
```

HangfireConfiguration.cs

```csharp
[Configuration("hangfire")]
public class HangfireConfiguration
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Database { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? DashboardUser { get; set; }
    public string? DashboardPassword { get; set; }
    public bool InMemory { get; set; }
    public int? PollingIntervalInMs { get; set; }
    public int? WorkerCount { get; set; }
    public string ConnectionString => $"Host={Host};Port={Port};Database={Database};Username={User};Password={Password};";
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

### Execution modes

If you want the job only to be executed as one instance at any given point in time use the

UniquePerQueueAttribute

this ensures that there is only one job of the same type/name in processing or queued at any given point!
