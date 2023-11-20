using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing.Models;

namespace PiBox.Plugins.Persistence.MongoDb
{
    public record TestEntity(Guid Id, string Name, DateTime CreationDate) : BaseTestEntity(Id, Name, CreationDate),
        IGuidIdentifier, ICreationDate;
}
