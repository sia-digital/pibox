using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.S3.Tests
{
    internal class SampleDataBlobObject : IBlobIdentifier, IBlobContent
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
        public Stream Data { get; set; }
        public DateTime? Expires { get; set; }
        public Dictionary<string, string> MetaData { get; set; }

        internal static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
