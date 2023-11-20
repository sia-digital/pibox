# PiBox.Hosting.Abstractions

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Hosting.Abstractions is the `core abstraction/interface package` that allows `dotnet dev's` to `develop their own plugins`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Hosting.Abstractions
```

or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Hosting.Abstractions" Version="" />
```

## ConfigurationAttribute

The configuration attribute can be used to easily create an configuration class for your plugin. The class decorated with the attribute is automatically bound on startup to the their corresponding setting values from appsettings files or ENV variables.

```csharp
example:
  mykey: "myvalue"
```

```csharp
using PiBox.Hosting.Abstractions.Attributes;
[Configuration("example")]
public class ExampleConfiguration
{
    public string MyKey { get; set; };

}
```

the class then can be consumed in any plugin via their constructor

```csharp
public class WebHostSamplePlugin
{
    private readonly ExampleConfiguration _exampleConfiguration;

    public WebHostSamplePlugin(ExampleConfiguration exampleConfiguration)
    {
        _exampleConfiguration = exampleConfiguration;
    }
}
```

## Plugin application configuration

This allows you in instruct the web host to execute certain methods on the application builder while the web host is starting. This is mainly used for `UseXYZ` extensions methods which usually are required by other 3rd party nuget packages.

```csharp
public class WebHostSamplePlugin : IPluginApplicationConfiguration
{
    public void ConfigureApplication(IApplicationBuilder applicationBuilder)
    {
        // register stuff on the application builder which should be executed if your plugin is loaded and used by an webhost
        // for example
        applicationBuilder.UseRouting();
    }
}
```

## Plugin service configuration

This allows you in instruct the web host to execute certain methods on the service collection while the web host is starting This is mainly used for IoC/DI registrations or other service setup methods.

```csharp
public class WebHostSamplePlugin : IPluginServiceConfiguration
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        // register stuff on the serviceCollcection which should be executed if your plugin is loaded and used by an webhost
        // for example
        serviceCollection.AddTransient<IMyClass, MyClass>();
    }
}
```

## Plugin health checks configuration & health check attribute

this enables the registration of health checks for which are exposed by the web host. The usage follows the documentation of the asp.net core health checks.

```csharp
public class WebHostSamplePlugin : IPluginServiceConfiguration
{
    public void ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
    {
        // for example
        var uriBuilder = new UriBuilder("baseUri") { Path = $"/checkTHIS" };
        var uri = uriBuilder.Uri;
        healthChecksBuilder.AddUrlGroup(uri, "mycheck", HealthStatus.Unhealthy, new[] { HealthCheckTags.Readiness });
    }
}
```

the same can be achieved by decorating an health implemented class with the attributes

```csharp
[ReadinessCheck("kafka")]
[LiveinessCheck("kafka")]
public class KafkaHealthCheck : IHealthCheck
{
        private readonly ClientConfig _clientConfig;
        private readonly ILogger<KafkaHealthCheck> _logger;

        public KafkaHealthCheck(ClientConfig clientConfig, ILogger<KafkaHealthCheck> logger)
        {
            _clientConfig = clientConfig;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            if (healthy == true)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Kafka is available."));
            }
            return Task.FromResult(HealthCheckResult.Unhealthy($"Kafka is unavailable. All servers are down."));
        }

}
```

## Plugin endpoints configuration

This enables the usage of dotnet 6 minimal apis. The official documentation of asp core minimal apis is valid and applicable

```csharp
public class WebHostSamplePlugin : IPluginEndpointsConfiguration
{
    public void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder, IServiceProvider serviceProvider)
    {
        // for example
        endpointRouteBuilder.MapGet("/hello", async () =>
        {
            return "Hello, World!";
        }).RequireAuthorization();
    }
}
```

## Plugin controller configuration

This enables the configuration of the controllers of your plugin

```csharp
public class WebHostSamplePlugin : IPluginControllerConfiguration
{
    public void ConfigureControllers(IMvcBuilder controllerBuilder)
    {
        // for example
        controllerBuilder.AddMvcOptions(options => options.RespectBrowserAcceptHeader = true);
    }
}
```

## Metrics

PiBox uses open telemetry for it's metrics capabilities with some wrappers for better developer experience Generally speaking anything from the [open telemetry documentation](https://opentelemetry.io/docs/instrumentation/net/) should be applicable and valid.

Simple metrics can be created and used like this

```csharp
Metrics.Meter.CreateCounter<long>($"my_metric_name_total", "calls").Add(1);
```

or with tags

```csharp
Metrics.Meter.CreateCounter<long>($"my_metric_name_total", "calls").Add(1, new KeyValuePair<string, object>("label", "custom-tag-value"));
```

as pre defined field

```csharp
private readonly Histogram<long> _commandDurationInSeconds = Metrics.Meter.CreateHistogram<long>("command_duration_seconds", "items", "description");
...
_commandDurationInSeconds.Add(1);
```

Default exposed metrics

```python
# TYPE http_client_duration_ms histogram
# UNIT http_client_duration_ms ms
# HELP http_client_duration_ms Measures the duration of outbound HTTP requests.
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="0"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="5"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="10"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="25"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="50"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="75"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="100"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="250"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="500"} 0 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="750"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="1000"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="2500"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="5000"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="7500"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="10000"} 1 1696509787492
http_client_duration_ms_bucket{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example",le="+Inf"} 1 1696509787492
http_client_duration_ms_sum{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example"} 590.7966 1696509787492
http_client_duration_ms_count{http_flavor="1.1",http_method="GET",http_scheme="https",http_status_code="200",net_peer_name="keycloak.example"} 1 1696509787492

# TYPE process_runtime_dotnet_gc_collections_count counter
# HELP process_runtime_dotnet_gc_collections_count Number of garbage collections that have occurred since process start.
process_runtime_dotnet_gc_collections_count{generation="gen2"} 0 1696509787492
process_runtime_dotnet_gc_collections_count{generation="gen1"} 0 1696509787492
process_runtime_dotnet_gc_collections_count{generation="gen0"} 0 1696509787492

# TYPE process_runtime_dotnet_gc_objects_size_bytes gauge
# UNIT process_runtime_dotnet_gc_objects_size_bytes bytes
# HELP process_runtime_dotnet_gc_objects_size_bytes Count of bytes currently in use by objects in the GC heap that haven't been collected yet. Fragmentation and other GC committed memory pools are excluded.
process_runtime_dotnet_gc_objects_size_bytes 21076568 1696509787492

# TYPE process_runtime_dotnet_gc_allocations_size_bytes counter
# UNIT process_runtime_dotnet_gc_allocations_size_bytes bytes
# HELP process_runtime_dotnet_gc_allocations_size_bytes Count of bytes allocated on the managed GC heap since the process start. .NET objects are allocated from this heap. Object allocations from unmanaged languages such as C/C++ do not use this heap.
process_runtime_dotnet_gc_allocations_size_bytes 21031848 1696509787492

# TYPE process_runtime_dotnet_jit_il_compiled_size_bytes counter
# UNIT process_runtime_dotnet_jit_il_compiled_size_bytes bytes
# HELP process_runtime_dotnet_jit_il_compiled_size_bytes Count of bytes of intermediate language that have been compiled since the process start.
process_runtime_dotnet_jit_il_compiled_size_bytes 556591 1696509787492

# TYPE process_runtime_dotnet_jit_methods_compiled_count counter
# HELP process_runtime_dotnet_jit_methods_compiled_count The number of times the JIT compiler compiled a method since the process start. The JIT compiler may be invoked multiple times for the same method to compile with different generic parameters, or because tiered compilation requested different optimization settings.
process_runtime_dotnet_jit_methods_compiled_count 7699 1696509787492

# TYPE process_runtime_dotnet_jit_compilation_time_ns counter
# UNIT process_runtime_dotnet_jit_compilation_time_ns ns
# HELP process_runtime_dotnet_jit_compilation_time_ns The amount of time the JIT compiler has spent compiling methods since the process start.
process_runtime_dotnet_jit_compilation_time_ns 1453127800 1696509787492

# TYPE process_runtime_dotnet_monitor_lock_contention_count counter
# HELP process_runtime_dotnet_monitor_lock_contention_count The number of times there was contention when trying to acquire a monitor lock since the process start. Monitor locks are commonly acquired by using the lock keyword in C#, or by calling Monitor.Enter() and Monitor.TryEnter().
process_runtime_dotnet_monitor_lock_contention_count 33 1696509787492

# TYPE process_runtime_dotnet_thread_pool_threads_count gauge
# HELP process_runtime_dotnet_thread_pool_threads_count The number of thread pool threads that currently exist.
process_runtime_dotnet_thread_pool_threads_count 5 1696509787492

# TYPE process_runtime_dotnet_thread_pool_completed_items_count counter
# HELP process_runtime_dotnet_thread_pool_completed_items_count The number of work items that have been processed by the thread pool since the process start.
process_runtime_dotnet_thread_pool_completed_items_count 53 1696509787492

# TYPE process_runtime_dotnet_thread_pool_queue_length gauge
# HELP process_runtime_dotnet_thread_pool_queue_length The number of work items that are currently queued to be processed by the thread pool.
process_runtime_dotnet_thread_pool_queue_length 0 1696509787492

# TYPE process_runtime_dotnet_timer_count gauge
# HELP process_runtime_dotnet_timer_count The number of timer instances that are currently active. Timers can be created by many sources such as System.Threading.Timer, Task.Delay, or the timeout in a CancellationSource. An active timer is registered to tick at some point in the future and has not yet been canceled.
process_runtime_dotnet_timer_count 5 1696509787492

# TYPE process_runtime_dotnet_assemblies_count gauge
# HELP process_runtime_dotnet_assemblies_count The number of .NET assemblies that are currently loaded.
process_runtime_dotnet_assemblies_count 215 1696509787492

# TYPE process_runtime_dotnet_exceptions_count counter
# HELP process_runtime_dotnet_exceptions_count Count of exceptions that have been thrown in managed code, since the observation started. The value will be unavailable until an exception has been thrown after OpenTelemetry.Instrumentation.Runtime initialization.
process_runtime_dotnet_exceptions_count 4 1696509787492

# TYPE process_memory_usage_By gauge
# UNIT process_memory_usage_By By
# HELP process_memory_usage_By The amount of physical memory allocated for this process.
process_memory_usage_By 147345408 1696509787492

# TYPE process_memory_virtual_By gauge
# UNIT process_memory_virtual_By By
# HELP process_memory_virtual_By The amount of committed virtual memory for this process.
process_memory_virtual_By 284054597632 1696509787492

# TYPE process_cpu_time_s counter
# UNIT process_cpu_time_s s
# HELP process_cpu_time_s Total CPU seconds broken down by different states.
process_cpu_time_s{state="user"} 2.46 1696509787492
process_cpu_time_s{state="system"} 0.39 1696509787492

# TYPE process_cpu_count__processors_ gauge
# UNIT process_cpu_count__processors_ _processors_
# HELP process_cpu_count__processors_ The number of processors (CPU cores) available to the current process.
process_cpu_count__processors_ 12 1696509787492

# TYPE process_threads__threads_ gauge
# UNIT process_threads__threads_ _threads_
# HELP process_threads__threads_ Process threads count.
process_threads__threads_ 61 1696509787492

# TYPE monitoring_logging_calls counter
# UNIT monitoring_logging_calls calls
# HELP monitoring_logging_calls count of logged messages
monitoring_logging_calls{log_level="Debug"} 37 1696509787492
monitoring_logging_calls{log_level="Information"} 6 1696509787492

# TYPE http_server_duration_ms histogram
# UNIT http_server_duration_ms ms
# HELP http_server_duration_ms Measures the duration of inbound HTTP requests.
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="0"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="5"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="10"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="25"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="50"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="75"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="100"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="250"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="500"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="750"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="1000"} 0 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="2500"} 1 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="5000"} 1 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="7500"} 1 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="10000"} 1 1696509787492
http_server_duration_ms_bucket{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300",le="+Inf"} 1 1696509787492
http_server_duration_ms_sum{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300"} 1180.9743 1696509787492
http_server_duration_ms_count{azp="my-clientid",http_flavor="1.1",http_method="GET",http_route="/hello",http_scheme="http",http_status_code="200",net_host_name="localhost",net_host_port="5300"} 1 1696509787492

# TYPE hangfire_job_count_Succeeded_calls gauge
# UNIT hangfire_job_count_Succeeded_calls calls
# HELP hangfire_job_count_Succeeded_calls description
hangfire_job_count_Succeeded_calls 0 1696509787492

# TYPE hangfire_job_count_Failed_calls gauge
# UNIT hangfire_job_count_Failed_calls calls
# HELP hangfire_job_count_Failed_calls description
hangfire_job_count_Failed_calls 0 1696509787492

# TYPE hangfire_job_count_Scheduled_calls gauge
# UNIT hangfire_job_count_Scheduled_calls calls
# HELP hangfire_job_count_Scheduled_calls description
hangfire_job_count_Scheduled_calls 0 1696509787492

# TYPE hangfire_job_count_Processing_calls gauge
# UNIT hangfire_job_count_Processing_calls calls
# HELP hangfire_job_count_Processing_calls description
hangfire_job_count_Processing_calls 0 1696509787492

# TYPE hangfire_job_count_Enqueued_calls gauge
# UNIT hangfire_job_count_Enqueued_calls calls
# HELP hangfire_job_count_Enqueued_calls description
hangfire_job_count_Enqueued_calls 0 1696509787492

# TYPE hangfire_job_count_Deleted_calls gauge
# UNIT hangfire_job_count_Deleted_calls calls
# HELP hangfire_job_count_Deleted_calls description
hangfire_job_count_Deleted_calls 0 1696509787492

# TYPE hangfire_job_count_Servers_calls gauge
# UNIT hangfire_job_count_Servers_calls calls
# HELP hangfire_job_count_Servers_calls description
hangfire_job_count_Servers_calls 1 1696509787492

# TYPE hangfire_job_count_Queues_calls gauge
# UNIT hangfire_job_count_Queues_calls calls
# HELP hangfire_job_count_Queues_calls description
hangfire_job_count_Queues_calls 0 1696509787492

# TYPE hangfire_job_count_Recurring_calls gauge
# UNIT hangfire_job_count_Recurring_calls calls
# HELP hangfire_job_count_Recurring_calls description
hangfire_job_count_Recurring_calls 2 1696509787492

# TYPE hangfire_job_count_RetryJobs_calls gauge
# UNIT hangfire_job_count_RetryJobs_calls calls
# HELP hangfire_job_count_RetryJobs_calls description
hangfire_job_count_RetryJobs_calls 0 1696509787492

# TYPE authentication_keycloak_success_calls counter
# UNIT authentication_keycloak_success_calls calls
# HELP authentication_keycloak_success_calls total count of authentication attempts with successful result
authentication_keycloak_success_calls 1 1696509787492

# EOF
```

## PiBoxException

//TODO

## Date abstraction

PiBox used [chronos.net](https://github.com/vfabing/Chronos.Net) as an datetime abstraction layer to enable test ability of implementations.
