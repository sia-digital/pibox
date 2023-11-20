using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Tests
{
    public record TestEntity(Guid Id, string Name) : IGuidIdentifier
    {
        public Guid Id { get; set; } = Id;
    }
}
