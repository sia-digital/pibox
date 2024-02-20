using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PiBox.Testing.Assertions;
using PiBox.Testing.Extensions;

namespace PiBox.Plugins.Persistence.S3.Tests
{
    [TestFixture]
    public class S3BlobStorageTests
    {
        private const string BucketName = "sample-bucket";
        private const string Key = "sample-object";
        private const string ContentType = "application/pdf";
        private IObjectOperations _s3Client = null!;
        private ILogger<S3BlobStorage> _logger = null!;
        private S3BlobStorage _blobStorage = null!;

        [SetUp]
        public void Init()
        {
            _s3Client = Substitute.For<IObjectOperations>();
            _logger = new FakeLogger<S3BlobStorage>();
            _blobStorage = new(_s3Client, _logger);
        }

        private static string GetBucket(object obj) => obj.GetInaccessibleValue<string>("BucketName");
        private static string GetObject(object obj) => obj.GetInaccessibleValue<string>("ObjectName");

        private static bool IsCorrectRequest(object o) => GetBucket(o) == BucketName && GetObject(o) == Key;

        [Test]
        public async Task GetObjectAsyncSuccessShouldWork()
        {
            await using var stream = SampleDataBlobObject.GenerateStreamFromString("my-fancy-file-content");
            stream.Seek(0, SeekOrigin.Begin);

            var objectStat = ObjectStat.FromResponseHeaders(Key, new Dictionary<string, string> { { BlobMetaData.ContentType, ContentType } });

            _s3Client.GetObjectAsync(Arg.Is<GetObjectArgs>(s => IsCorrectRequest(s)))
                .Returns((getObjectArgs) =>
                {
                    var callback = getObjectArgs.Arg<GetObjectArgs>().GetInaccessibleValue<Func<Stream, CancellationToken, Task>>("CallBack");
                    // ReSharper disable once AccessToDisposedClosure
                    callback.Invoke(stream, CancellationToken.None).Wait();
                    return Task.FromResult(objectStat);
                });

            var value = await _blobStorage.GetObjectAsync(BucketName, Key);
            value.Should().NotBeNull();
            value.MetaData[BlobMetaData.ContentType].Should().Be(ContentType);

            (await new StreamReader(value.Data).ReadToEndAsync()).Should().Be("my-fancy-file-content");
        }

        [Test]
        public async Task GetObjectAsyncSuccessShouldNotWork()
        {
            _s3Client.GetObjectAsync(Arg.Is<GetObjectArgs>(s => IsCorrectRequest(s)))
                .Throws(new Exception("test"));

            await _blobStorage.Invoking(async x => await x.GetObjectAsync(BucketName, Key))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task DeleteObjectAsyncShouldWork()
        {
            var objectStat = ObjectStat.FromResponseHeaders(Key, new Dictionary<string, string> { { BlobMetaData.ContentType, ContentType } });

            _s3Client.RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(objectStat));

            await _blobStorage.Invoking(async x => await x.DeleteObjectAsync(BucketName, Key)).Should().NotThrowAsync();
        }

        [Test]
        public async Task DeleteObjectAsyncSuccessShouldNotWork()
        {
            _s3Client.RemoveObjectAsync(Arg.Any<RemoveObjectArgs>(), Arg.Any<CancellationToken>())
                .Throws(new Exception("test"));

            await _blobStorage.Invoking(async x => await x.DeleteObjectAsync(BucketName, Key))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task PutObjectAsyncShouldWork()
        {
            _s3Client.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
                .Returns(new PutObjectResponse(HttpStatusCode.OK, "", new Dictionary<string, string> { { BlobMetaData.ContentType, ContentType } }, 0, Key));

            var blobObject = new SampleDataBlobObject();
            blobObject.Bucket = BucketName;
            blobObject.Key = Key;
            blobObject.MetaData = new() { { BlobMetaData.ContentType, ContentType } };
            blobObject.Data = new MemoryStream();

            await _blobStorage.Invoking(async x => await x.PutObjectAsync(blobObject, CancellationToken.None)).Should()
                .NotThrowAsync();
        }

        [Test]
        public async Task PutObjectAsyncSuccessShouldNotWork()
        {
            _s3Client.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
                .Throws(new Exception("test"));
            var blobObject = new SampleDataBlobObject
            {
                Bucket = BucketName,
                Key = Key,
                MetaData = new() { { BlobMetaData.ContentType, ContentType } },
                Data = new MemoryStream()
            };
            await _blobStorage.Invoking(async x => await x.PutObjectAsync(blobObject, CancellationToken.None))
                .Should().ThrowAsync<Exception>();
        }
    }
}
