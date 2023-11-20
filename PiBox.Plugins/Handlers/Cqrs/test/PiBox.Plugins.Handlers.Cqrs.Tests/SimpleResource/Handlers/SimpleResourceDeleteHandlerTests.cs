using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Exceptions;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Models;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Handlers.Cqrs.Tests.SimpleResource.Handlers
{
    public class SimpleResourceDeleteHandlerTests
    {
        private IRepository<TestEntity> _repository = null!;
        private IDeleteHandler<TestEntity> _handler = null!;

        [SetUp]
        public void Init()
        {
            ActivityTestBootstrapper.Setup();
            _repository = Substitute.For<IRepository<TestEntity>>();
            _handler = new PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers.SimpleResourceDeleteHandler<TestEntity>(_repository);
        }

        [Test]
        public async Task ReturnsAValidationErrorIfRequestIsInvalid()
        {
            var identifier = GuidIdentifier.Empty;
            await _handler.Invoking(async x => await x.DeleteAsync(identifier, CancellationToken.None))
                .Should().ThrowAsync<ValidationPiBoxException>();
        }

        [Test]
        public async Task DeleteWorks()
        {
            var identifier = GuidIdentifier.New;
            await _handler.DeleteAsync(identifier, CancellationToken.None);
            await _repository.Received(1).RemoveAsync(identifier.Id, CancellationToken.None);
        }
    }
}
