using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Text;
using EzSmb;
using EzSmb.Params;
using Microsoft.Extensions.Logging;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Hosting.Abstractions.Metrics;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.Smb
{
    [ExcludeFromCodeCoverage(Justification = "Has no suitable way to test it")]
    internal class SmbStorage : ISmbStorage
    {
        private readonly Counter<long> _writeCounter = Metrics.CreateCounter<long>("pibox_persistence_smb_writes_total",
            "calls", "amount of total write calls");

        private readonly Counter<long> _readCounter = Metrics.CreateCounter<long>("pibox_persistence_smb_reads_total",
            "calls", "amount of total read calls");

        private readonly Counter<long> _deleteCounter = Metrics.CreateCounter<long>("pibox_persistence_smb_deletes_total",
            "calls", "amount of total delete calls");

        private readonly ParamSet _smbParamSet;
        private readonly SmbStorageConfiguration _smbStorageConfiguration;
        private readonly ILogger<SmbStorage> _logger;

        public SmbStorage(SmbStorageConfiguration smbStorageConfiguration, ILogger<SmbStorage> logger)
        {
            _smbParamSet = new ParamSet
            {
                Password = smbStorageConfiguration.Password,
                UserName = smbStorageConfiguration.Username,
                DomainName = smbStorageConfiguration.Domain
            };
            _smbStorageConfiguration = smbStorageConfiguration;
            _logger = logger;
        }

        private static string CorrectDelimiters(string path) =>
            path?.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');

        internal async Task<string> GetSmbPath(string path = null)
        {
            path = CorrectDelimiters(path);
            var smbPathBuilder = new StringBuilder();
            smbPathBuilder.Append(_smbStorageConfiguration.Server);
            var driveMapping = path is null
                ? null
                : _smbStorageConfiguration.DriveMappings.FirstOrDefault(x =>
                    path.StartsWith(x.Drive, StringComparison.InvariantCultureIgnoreCase));
            var share = "";
            if (driveMapping is not null)
            {
                share = driveMapping.Share;
                path = CorrectDelimiters(path.Replace(driveMapping.Drive, ""));
            }

            var mapping = _smbStorageConfiguration.ShareMappings.FirstOrDefault(x =>
                path?.StartsWith(x.Folder, StringComparison.InvariantCultureIgnoreCase) ??
                false);
            if (mapping != null)
            {
                path = string.Join("\\", path!.Split('\\').Skip(1));
                smbPathBuilder.Append("\\" + mapping.Share);
            }
            else if (share != "")
            {
                smbPathBuilder.Append("\\" + share);
            }
            else
            {
                var pathParts = path?.Split('\\') ?? [];
                var isCorrectNode = pathParts.Length != 0 &&
                                    await Node.GetNode($"{smbPathBuilder}\\{pathParts[0]}", _smbParamSet) != null;
                if (!isCorrectNode)
                    smbPathBuilder.Append("\\" + _smbStorageConfiguration.DefaultShare);
            }

            if (string.IsNullOrWhiteSpace(path))
                return smbPathBuilder.ToString();

            smbPathBuilder.Append('\\');
            smbPathBuilder.Append(path);
            return smbPathBuilder.ToString();
        }

        private async Task<Node> GetNode(string path = null)
        {
            Node node;
            try
            {
                node = await Node.GetNode(path, _smbParamSet, true);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Smb exception occured");
                throw new FileNotFoundException(
                    $"Could not find file on smb associated to relative path '{path}'", path);
            }
            return node;
        }

        public Task WriteAsync(
            string filePath,
            string content,
            CancellationToken cancellationToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return WriteAsync(filePath, bytes, cancellationToken);
        }

        public async Task WriteAsync(
            string filePath,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                memoryStream.Write(buffer, 0, read);
            await WriteAsync(filePath, memoryStream.ToArray(), cancellationToken);
        }

        public async Task<string> ReadStringAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = await ReadStreamAsync(filePath, cancellationToken);
            using var streamReader = new StreamReader(fileStream);
            return await streamReader.ReadToEndAsync(cancellationToken);
        }

        public async Task<byte[]> ReadBytesAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = await ReadStreamAsync(filePath, cancellationToken);
            using var memoryStream = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
                memoryStream.Write(buffer, 0, read);
            return memoryStream.ToArray();
        }

        public async Task<Stream> ReadStreamAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var file = await GetNode(await GetSmbPath(filePath));
            _readCounter.Add(1,
                new KeyValuePair<string, object>("share", file.PathSet.Share),
                new KeyValuePair<string, object>("size", file.Size)
            );
            return await file.Read();
        }

        public async Task EnsurePathAsync(string path, CancellationToken cancellationToken = default)
        {
            var realPath = (await GetSmbPath(path)).Split(Path.DirectorySeparatorChar);
            var node = await GetNode(realPath[0]);
            for (var i = 1; i < realPath.Length; i++)
            {
                var nodePath = string.Join(Path.DirectorySeparatorChar, realPath.Take(i + 1));
                var newNode = await Node.GetNode(nodePath, _smbParamSet);
                node = newNode ?? await node.CreateFolder(realPath[i]);
            }
        }

        public async Task DeleteIfExists(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var node = await GetNode(filePath);
                if (node is not null)
                {
                    var isDeleted = !await node.Delete();
                    if (!isDeleted)
                        throw new PiBoxException($"Could not delete SMB Node: '{filePath}'");
                    _deleteCounter.Add(1,
                        new KeyValuePair<string, object>("share", node.PathSet.Share),
                        new KeyValuePair<string, object>("size", node.Size)
                    );
                }
            }
            catch (FileNotFoundException)
            {
                // ignore
            }
        }

        public async Task WriteAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
        {
            var filePaths = filePath.Replace('/', '\\').TrimStart('\\').Split('\\');
            var folderPath = string.Join("\\", filePaths.Take(filePaths.Length - 1));
            var fileName = filePaths.Last();
            var folder = await GetNode(await GetSmbPath(folderPath));
            using var memStream = new MemoryStream(content);
            var isWritten = await folder.Write(memStream, fileName);
            if (!isWritten) throw new InvalidOperationException("Could not write file");
            _writeCounter.Add(1,
                new KeyValuePair<string, object>("share", folder.PathSet.Share),
                new KeyValuePair<string, object>("size", memStream.Length)
            );
        }
    }
}
