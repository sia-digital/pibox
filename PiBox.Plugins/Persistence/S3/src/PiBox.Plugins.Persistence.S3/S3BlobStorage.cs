using Microsoft.Extensions.Logging;
using Minio;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.S3
{
    public class S3BlobStorage : IBlobStorage
    {
        private readonly IObjectOperations _objectOperations;
        private readonly ILogger _logger;

        public S3BlobStorage(IObjectOperations objectOperations, ILogger<S3BlobStorage> logger)
        {
            _objectOperations = objectOperations;
            _logger = logger;
        }

        public async Task<IBlobContent> GetObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
        {
            var blobContent = new S3BlobContent { Data = new MemoryStream() };
            var getArgs = new GetObjectArgs().WithBucket(bucket).WithObject(key).WithCallbackStream(stream => stream.CopyTo(blobContent.Data));
            var objectStat = await _objectOperations.GetObjectAsync(getArgs, cancellationToken);
            blobContent.Data.Seek(0, SeekOrigin.Begin);
            blobContent.MetaData = objectStat.MetaData;
            return blobContent;
        }

        public async Task PutObjectAsync<T>(T blobContent, CancellationToken cancellationToken = default)
            where T : IBlobIdentifier, IBlobContent
        {
            var arguments = new PutObjectArgs()
                .WithBucket(blobContent.Bucket)
                .WithObject(blobContent.Key)
                .WithStreamData(blobContent.Data)
                .WithObjectSize(blobContent.Data.Length)
                .WithContentType(blobContent.MetaData[BlobMetaData.ContentType]);

            await _objectOperations.PutObjectAsync(arguments, cancellationToken);
            _logger.LogDebug("S3: Put {Object} into {Bucket}", blobContent.Key, blobContent.Bucket);
        }

        public async Task DeleteObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
        {
            var arguments = new RemoveObjectArgs().WithBucket(bucket).WithObject(key);
            await _objectOperations.RemoveObjectAsync(arguments, cancellationToken);
            _logger.LogDebug("S3: Put {Object} into {Bucket}", key, bucket);
        }
    }
}
