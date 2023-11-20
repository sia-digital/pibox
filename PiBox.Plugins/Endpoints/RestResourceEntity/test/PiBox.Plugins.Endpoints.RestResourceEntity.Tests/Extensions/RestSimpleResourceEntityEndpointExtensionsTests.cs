using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Endpoints.RestResourceEntity.Extensions;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Tests.Extensions
{
    public class RestSimpleResourceEntityEndpointExtensionsTests
    {
        [Test]
        public void AddSimpleRestResourceWorks()
        {
            var endpointBuilder = Substitute.For<IEndpointRouteBuilder>();
            var sp = Substitute.For<IServiceProvider>();
            sp.GetService(typeof(GlobalResponseOptions)).Returns(new GlobalResponseOptions());
            var builder = endpointBuilder.AddSimpleRestResource<TestEntity>(sp, "test-entities");
            builder.Should().NotBeNull();
        }
    }
}
