# PiBox.Plugins.Persistence.S3

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)

PiBox.Plugins.Persistence.S3 is a `plugin` that allows other `pibox components` to do `interact witha s3 compatible storage`.

## Installation

To install the nuget package follow these steps:

```shell
dotnet add package PiBox.Plugins.Persistence.S3
```
or add as package reference to your .csproj

```xml
<PackageReference Include="PiBox.Plugins.Persistence.S3" Version="" />
```

## Appsettings.yml

Configure your appsettings.yml with these properties

```yaml
s3:
  endpoint: ""
  accessKey: ""
  secretKey: ""
  region: ""
  useSsl: true
  healthCheckPath: ""
```

## Usage

```csharp
public interface IExampleService
{
    Task PutObjectAsync();
    Task GetObjectAsync();
    Task DeleteObjectAsync()
}

public class ExampleService : IExampleService
{
    private readonly IBlobStorage _blobStorage;

    // retrieve the registered service IBlobStorage from the DI container via depentency injection
    public ExampleService(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    // put/update a (new) object into the s3 storage
    public async Task PutObjectAsync()
    {
        var sampleDataBlobObject = new SampleDataBlobObject();
        sampleDataBlobObject.Bucket = "BucketName";
        sampleDataBlobObject.Key = "Key";
        sampleDataBlobObject.MetaData = new() { { BlobMetaData.ContentType, "application/pdf" } };
        sampleDataBlobObject.Data = SampleDataBlobObject.GenerateStreamFromString("my-fancy-file-content");

        var result = await _blobStorage.PutObjectAsync(sampleDataBlobObject);
    }

    // retrieve a object from the s3 storage
    public async Task GetObjectAsync()
    {
        var result = await _blobStorage.GetObjectAsync("bucketName", "Key", CancellationToken.None);
        var fileContent = new StreamReader(result.Value.Data).ReadToEnd();
    }

    // delete a object from the s3 storage
    public async Task DeleteObjectAsync()
    {
        var result = await blobStorage.DeleteObjectAsync("BucketName", "Key", CancellationToken.None);
    }
}
```
