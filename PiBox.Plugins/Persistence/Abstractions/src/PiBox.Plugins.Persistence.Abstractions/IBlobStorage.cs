namespace PiBox.Plugins.Persistence.Abstractions
{
    public interface IBlobStorage
    {
        Task<IBlobContent> GetObjectAsync(string bucket, string key, CancellationToken cancellationToken = default);

        Task<IBlobContent> GetObjectAsync(IBlobIdentifier identifier, CancellationToken cancellationToken) =>
            GetObjectAsync(identifier.Bucket, identifier.Key, cancellationToken);

        Task PutObjectAsync<T>(T blobContent, CancellationToken cancellationToken = default) where T : IBlobIdentifier, IBlobContent;
        Task DeleteObjectAsync(string bucket, string key, CancellationToken cancellationToken = default);

        Task DeleteObjectAsync(IBlobIdentifier identifier, CancellationToken cancellationToken = default) =>
            DeleteObjectAsync(identifier.Bucket, identifier.Key, cancellationToken);
    }
}
