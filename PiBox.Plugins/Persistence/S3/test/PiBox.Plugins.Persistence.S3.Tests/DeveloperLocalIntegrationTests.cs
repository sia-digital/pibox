using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Persistence.S3.Tests
{
    [Explicit("Needs a real s3 connection")]
    public class DeveloperLocalIntegrationTests
    {
        private const string BucketName = "sample-bucket";
        private const string Key = "sample-object";
        private const string ContentType = "application/pdf";
#pragma warning disable S6290
        private readonly S3Configuration _configuration = new()
        {
            Endpoint = "play.min.io",
            AccessKey = "Q3AM3UQ867SPQQA43P2F",
            SecretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG",
            UseSsl = true
        };
#pragma warning restore S6290
        private S3Plugin _plugin = null!;

        [SetUp]
        public void Init()
        {
            _plugin = new S3Plugin(_configuration);
        }

        [Test]
        public async Task PutObjectAsync()
        {
            var sc = TestingDefaults.ServiceCollection();
            _plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();

            var blobStorage = sp.GetRequiredService<IBlobStorage>();
            var sampleDataBlobObject = new SampleDataBlobObject
            {
                Bucket = BucketName,
                Key = Key,
                MetaData = new Dictionary<string, string> { { BlobMetaData.ContentType, ContentType } },
                Data = SampleDataBlobObject.GenerateStreamFromString("my-fancy-file-content")
            };

            await blobStorage.Invoking(async x => await x.PutObjectAsync(sampleDataBlobObject)).Should()
                .NotThrowAsync();
        }

        [Test]
        public async Task GetObjectAsync()
        {
            var sc = TestingDefaults.ServiceCollection();
            _plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();

            var blobStorage = sp.GetRequiredService<IBlobStorage>();

            var blobContent = await blobStorage.GetObjectAsync(BucketName, Key, CancellationToken.None);
            (await new StreamReader(blobContent.Data).ReadToEndAsync()).Should().Be("my-fancy-file-content");
            blobContent.MetaData[BlobMetaData.ContentType].Should().Be(ContentType);
        }

        [Test]
        public async Task DeleteObjectAsync()
        {
            var sc = TestingDefaults.ServiceCollection();
            _plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider();

            var blobStorage = sp.GetRequiredService<IBlobStorage>();

            await blobStorage.Invoking(async x => await x.DeleteObjectAsync(BucketName, Key, CancellationToken.None))
                .Should().NotThrowAsync();
        }

    }
}
