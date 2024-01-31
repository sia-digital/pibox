using System.Text;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.Smb
{
    internal static class SmbStorageExtensions
    {
        public static Task WriteStringAsync(this ISmbStorage smbStorage,
            string filePath,
            string content,
            CancellationToken cancellationToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return smbStorage.WriteAsync(filePath, bytes, cancellationToken);
        }

        public static async Task WriteStreamAsync(this ISmbStorage smbStorage,
            string filePath,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                memoryStream.Write(buffer, 0, read);
            await smbStorage.WriteAsync(filePath, memoryStream.ToArray(), cancellationToken);
        }

        public static async Task<string> ReadStringAsync(this ISmbStorage smbStorage,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = await smbStorage.ReadStreamAsync(filePath, cancellationToken);
            using var streamReader = new StreamReader(fileStream);
            return await streamReader.ReadToEndAsync(cancellationToken);
        }

        public static async Task<byte[]> ReadBytesAsync(this ISmbStorage smbStorage,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = await smbStorage.ReadStreamAsync(filePath, cancellationToken);
            using var memoryStream = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
                memoryStream.Write(buffer, 0, read);
            return memoryStream.ToArray();
        }
    }
}
