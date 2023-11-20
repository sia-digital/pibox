using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.InMemory.Tests
{
    public record SampleModel : IGuidIdentifier, ICreationDate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
