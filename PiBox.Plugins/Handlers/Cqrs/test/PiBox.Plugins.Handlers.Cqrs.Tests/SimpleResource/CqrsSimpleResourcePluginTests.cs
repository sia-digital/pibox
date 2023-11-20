using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Services;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource;
using PiBox.Plugins.Handlers.Cqrs.SimpleResource.Handlers;
using PiBox.Plugins.Persistence.Abstractions;
using PiBox.Testing;

namespace PiBox.Plugins.Handlers.Cqrs.Tests.SimpleResource
{
    public class CqrsSimpleResourcePluginTests
    {
        private IImplementationResolver _implementationResolver = null!;
        private CqrsSimpleResourcePlugin _plugin = null!;
        private IServiceCollection _serviceCollection = null!;
        private IRepository<TestEntity> _repository = null!;

        [SetUp]
        public void Init()
        {
            _implementationResolver = Substitute.For<IImplementationResolver>();
            _plugin = new CqrsSimpleResourcePlugin(_implementationResolver, Substitute.For<ILogger<CqrsSimpleResourcePlugin>>());
            _repository = Substitute.For<IRepository<TestEntity>>();
            _serviceCollection = TestingDefaults.ServiceCollection();
            _serviceCollection.AddSingleton(_repository);
            _serviceCollection.AddSingleton<IReadRepository<TestEntity>>(_repository);
        }

        [Test]
        public void SimpleResourceHandlersAreRegistered()
        {
            _implementationResolver.FindTypes().Returns(new List<Type> { typeof(CustomValidator) });
            _plugin.ConfigureServices(_serviceCollection);
            var sp = _serviceCollection.BuildServiceProvider();

            var createHandler = sp.GetRequiredService<ICreateHandler<TestEntity>>();
            createHandler.Should().NotBeNull();
            createHandler.Should().BeOfType<SimpleResourceCreateHandler<TestEntity>>();

            var updateHandler = sp.GetRequiredService<IUpdateHandler<TestEntity>>();
            updateHandler.Should().NotBeNull();
            updateHandler.Should().BeOfType<SimpleResourceUpdateHandler<TestEntity>>();

            var deleteHandler = sp.GetRequiredService<IDeleteHandler<TestEntity>>();
            deleteHandler.Should().NotBeNull();
            deleteHandler.Should().BeOfType<SimpleResourceDeleteHandler<TestEntity>>();

            var getHandler = sp.GetRequiredService<IGetHandler<TestEntity>>();
            getHandler.Should().NotBeNull();
            getHandler.Should().BeOfType<SimpleResourceGetHandler<TestEntity>>();

            var getListHandler = sp.GetRequiredService<IGetListHandler<TestEntity>>();
            getListHandler.Should().NotBeNull();
            getListHandler.Should().BeOfType<SimpleResourceGetListHandler<TestEntity>>();
        }

        [Test]
        public void HandlerCanBeOverriden()
        {
            _implementationResolver.FindTypes().Returns(new List<Type> { typeof(CustomValidator), typeof(CustomDeleteHandler) });
            _plugin.ConfigureServices(_serviceCollection);
            var sp = _serviceCollection.BuildServiceProvider();

            var deleteHandler = sp.GetRequiredService<IDeleteHandler<TestEntity>>();
            deleteHandler.Should().NotBeNull();
            deleteHandler.Should().BeOfType<CustomDeleteHandler>();
        }
    }
}
