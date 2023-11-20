using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using PiBox.Extensions.Abstractions;
using PiBox.Plugins.Endpoints.RestResourceEntity.Endpoints;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Handlers;
using PiBox.Plugins.Handlers.Abstractions.Cqrs.Models;
using PiBox.Testing;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Tests.Endpoints
{
    public class RestActionsTests
    {
        private IHttpContextAccessor _httpContextAccessor = null!;
        private HttpContext _httpContext = null!;
        private MemoryStream _responseStream = null!;
        private HttpResponse Response => _httpContext.Response;

        [SetUp]
        public void Setup()
        {
            _responseStream = new MemoryStream();
            _httpContext = new DefaultHttpContext();
            _httpContext.RequestServices = TestingDefaults.ServiceProvider();
            _httpContext.Response.Body = _responseStream;
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _httpContextAccessor.HttpContext.Returns(_httpContext);
        }

        private void SetMethod(HttpMethod method) => _httpContext.Request.Method = method.ToString().ToUpper(CultureInfo.InvariantCulture);

        private T GetResponseBody<T>()
        {
            _responseStream.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(_responseStream);
            var content = sr.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(content);
        }

        [Test]
        public async Task GetListAsyncReturnsResultAsHttpResult()
        {
            SetMethod(HttpMethod.Get);
            var entities = new[] { new TestEntity(Guid.NewGuid(), "test") };
            var handler = Substitute.For<IGetListHandler<TestEntity>>();
            handler.GetListAsync(Arg.Is<PagingRequest>(p => p.Size == 25 && p.Page == 0), Arg.Any<CancellationToken>())
                .Returns(new PagedList<TestEntity>(entities, 1, 0, 25));
            var result = await RestActions.GetListAsync(25, 0, null, null, handler, _httpContextAccessor, CancellationToken.None);
            await result.ExecuteAsync(_httpContext);
            Response.StatusCode.Should().Be(200);
            var body = GetResponseBody<PagedList<TestEntity>>()!;
            body.Should().NotBeNull();
            body.Page.Current.Should().Be(0);
            body.Page.Size.Should().Be(25);
            body.Page.TotalElements.Should().Be(1);
            body.Items.Should().HaveCount(1);
            body.Items[0].Id.Should().Be(entities[0].Id);
            body.Items[0].Name.Should().Be(entities[0].Name);
        }

        [Test]
        public async Task GetAsyncReturnsResultAsHttpResult()
        {
            SetMethod(HttpMethod.Get);
            var entity = new TestEntity(Guid.NewGuid(), "test");
            var handler = Substitute.For<IGetHandler<TestEntity>>();
            handler.GetAsync(new GuidIdentifier(entity.Id), CancellationToken.None).Returns(entity);
            var result = await RestActions.GetAsync(entity.Id, handler, _httpContextAccessor, CancellationToken.None);
            await result.ExecuteAsync(_httpContext);
            Response.StatusCode.Should().Be(200);
            var body = GetResponseBody<TestEntity>()!;
            body.Should().NotBeNull();
            body.Id.Should().Be(entity.Id);
            body.Name.Should().Be(entity.Name);
        }

        [Test]
        public async Task CreateAsyncReturnsResultAsHttpResult()
        {
            SetMethod(HttpMethod.Post);
            var entity = new TestEntity(Guid.NewGuid(), "test");
            var handler = Substitute.For<ICreateHandler<TestEntity>>();
            handler.CreateAsync(entity, CancellationToken.None).Returns(entity);
            var result = await RestActions.CreateAsync(entity, handler, _httpContextAccessor, CancellationToken.None);
            await result.ExecuteAsync(_httpContext);
            Response.StatusCode.Should().Be(200);
            var body = GetResponseBody<TestEntity>()!;
            body.Should().NotBeNull();
            body.Id.Should().Be(entity.Id);
            body.Name.Should().Be(entity.Name);
        }

        [Test]
        public async Task UpdateAsyncReturnsResultAsHttpResult()
        {
            SetMethod(HttpMethod.Put);
            var id = Guid.NewGuid();
            var entity = new TestEntity(Guid.Empty, "test");
            var handler = Substitute.For<IUpdateHandler<TestEntity>>();
            handler.UpdateAsync(Arg.Is<TestEntity>(t => t.Id == id), CancellationToken.None).Returns(entity);
            var result = await RestActions.UpdateAsync(id, entity, handler, _httpContextAccessor, CancellationToken.None);
            await result.ExecuteAsync(_httpContext);
            Response.StatusCode.Should().Be(200);
            var body = GetResponseBody<TestEntity>()!;
            body.Should().NotBeNull();
            body.Id.Should().Be(entity.Id);
            body.Name.Should().Be(entity.Name);
        }

        [Test]
        public async Task DeleteAsyncReturnsResultAsHttpResult()
        {
            SetMethod(HttpMethod.Delete);
            var id = Guid.NewGuid();
            var handler = Substitute.For<IDeleteHandler<TestEntity>>();
            var result = await RestActions.DeleteAsync(id, handler, _httpContextAccessor, CancellationToken.None);
            await result.ExecuteAsync(_httpContext);
            Response.StatusCode.Should().Be(204);
        }
    }
}
