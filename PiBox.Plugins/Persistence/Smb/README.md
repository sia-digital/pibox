# PiBox.Plugins.Persistence.Smb

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Persistence.Smb is a `plugin` that allows other `pibox components` to do `interact witha Smb compatible storage`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Persistence.Smb
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Persistence.Smb" Version="" />
```

## Appsettings.yml

Configure your appsettings.yml with these properties

```yaml
smb:
  server: example.com
  domain: mydomain.com
  defaultShare: myshare
  driveMappings:
    - drive: 'J:'
      share: myshare
  shareMappings:
    - folder: myfolder
      share: myshare
```

## Usage

```csharp
public class ExampleService
{
    private readonly ISmbStorage _smbStorage;

    // retrieve the registered service ISmbStorage from the DI container via depentency injection
    public ExampleService(ISmbStorage smbStorage)
    {
        _smbStorage = smbStorage;
    }

    // ensure a file exist in the smb storage
    public async Task EnsurePathAsync()
    {
        await _fileStorage.EnsurePathAsync("folder/subfolder/", cancellationToken);
    }

    // delete a file from the smb storage
    public async Task DeleteIfExists()
    {
        await _fileStorage.DeleteIfExists("folder/myfile.txt", cancellationToken);
    }

    // write a byte array as file content to the smb storage
    public async Task WriteAsync()
    {
        await _fileStorage.WriteAsync("folder/myfile.txt", new byte[0], cancellationToken);
    }

    // write a string as file content to the smb storage
    public async Task WriteStringAsync()
    {
        await _fileStorage.WriteStringAsync("folder/myfile.txt", "hello world", cancellationToken);
    }

    // write a stream as file content to the smb storage
    public async Task WriteStreamAsync()
    {
        await _fileStorage.WriteStringAsync("folder/myfile.txt", new MemoryStream(), cancellationToken);
    }

    // read a file form smb storage and return content as string
    public async Task ReadStringAsync()
    {
        string content = await ReadStreamAsync("folder/myfile.txt", cancellationToken);
    }

    // read a file form smb storage and return content as byte array
    public async Task ReadBytesAsync()
    {
        bytes[] stream = await ReadStreamAsync("folder/myfile.txt", cancellationToken);
    }
}
```
