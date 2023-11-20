# PiBox.Plugins.Persistence.EntityFramework

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Persistence.EntityFramework is a `plugin` that allows other `PiBox components` to `use the entityframework core to interact with certain databases`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Persistence.EntityFramework
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Persistence.EntityFramework" Version="" />
```

## Appsettings.yml

Configure your appsettings.yml with these properties

Postgres for example
```yaml
samplePostgresDb:
  host: "localhost"
  db: "samplePostgresDb"
  port: "5432"
  password: "postgres"
  user: "postgres"
```

PluginReadmeTemplateConfig.cs
```csharp
[Configuration("samplePostgresDb")]
public class NpgsqlDbConfiguration
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string Database { get; set; } = null!;
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
}
```

## Configuration in your plugin host

```csharp
public class PluginReadme : IPluginEndpointsConfiguration, IPluginServiceConfiguration, IPluginApplicationConfiguration, IPluginControllerConfiguration, IPluginHealthChecksConfiguration
{
    private readonly NpgsqlDbConfiguration _npgsqlDbConfiguration;

    public PluginReadme(NpgsqlDbConfiguration npgsqlDbConfiguration)
    {
        _npgsqlDbConfiguration = npgsqlDbConfiguration;
    }

    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        var connectionString = ConnectionStringBuilder(_npgsqlDbConfiguration);
        serviceCollection.AddEfContext<WebHostSamplePluginDbContext>(options => options.UseNpgsql(connectionString));
    }

    public void ConfigureApplication(IApplicationBuilder applicationBuilder)
    {
        // ensure your database gets initialized setup your EFCore migrations in your project and call
        applicationBuilder.ApplicationServices.GetRequiredService<WebHostSamplePluginDbContext>().Database.Migrate();
        // or call
        applicationBuilder.ApplicationServices.GetRequiredService<WebHostSamplePluginDbContext>().Database.EnsureCreated();
    }

    private static string ConnectionStringBuilder(NpgsqlDbConfiguration configuration)
    {
        return (new NpgsqlConnectionStringBuilder()
        { Host = configuration.Host, Database = configuration.Database, Username = configuration.User, Password = configuration.Password, Port = configuration.Port }).ConnectionString;
    }
}
```

## Usage

```csharp
internal class SamplePluginDbContext : DbContext, IDbContext<MyPoco>
{
    public SamplePluginDbContext(DbContextOptions<SamplePluginDbContext> options) : base(options) { }

    public DbContext GetContext() => this;

    public DbSet<MyPoco> GetSet() => Set<MyPoco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyPoco>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).IsRequired();
        });
        base.OnModelCreating(modelBuilder);
    }
}

public interface IExampleService
{
    void DoStuff();
}

public class ExampleService : IExampleService
{
    private readonly IReadRepository<MyPoco> _readRepository;

    public ExampleService(IReadRepository<MyPoco> readRepository)
    {
        _readRepository = readRepository;
    }
    public async Task DoStuff()
    {
        // for example
        var result = await _readRepository.FindByIdAsync();
    }
}
```


