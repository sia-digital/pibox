namespace PiBox.Plugins.Persistence.Abstractions
{
    public interface IBlobContent
    {
        Stream Data { get; }
        Dictionary<string, string> MetaData { get; set; }
    }
}
