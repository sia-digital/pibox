using System.Net;
using FluentAssertions;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Middlewares.Models;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Tests
{
    public class GlobalResponseOptionsTests
    {
        [Test]
        public void HasInitialDataSetup()
        {
            var options = new GlobalResponseOptions();

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.Unauthorized);
            options.DefaultResponses[HttpStatusCode.Unauthorized].Should().Be(typeof(ErrorResponse));

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.Forbidden);
            options.DefaultResponses[HttpStatusCode.Forbidden].Should().Be(typeof(ErrorResponse));

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.InternalServerError);
            options.DefaultResponses[HttpStatusCode.InternalServerError].Should().Be(typeof(ErrorResponse));

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.BadRequest);
            options.DefaultResponses[HttpStatusCode.BadRequest].Should().Be(typeof(ValidationErrorResponse));
        }

        [Test]
        public void CanEditTheGlobalResponseOptions()
        {
            var options = new GlobalResponseOptions();
            options.Add(HttpStatusCode.NoContent);
            options.Add<GlobalResponseOptionsTests>(HttpStatusCode.Processing);
            options.Remove(HttpStatusCode.InternalServerError);

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.NoContent);
            options.DefaultResponses[HttpStatusCode.NoContent].Should().BeNull();

            options.DefaultResponses.Should().ContainKey(HttpStatusCode.Processing);
            options.DefaultResponses[HttpStatusCode.Processing].Should().Be(typeof(GlobalResponseOptionsTests));

            options.DefaultResponses.Should().NotContainKey(HttpStatusCode.InternalServerError);
        }
    }
}
