using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Extensions;

namespace PiBox.Hosting.WebHost.Tests
{
    public class CorsPolicyExtensionTests
    {
        [Test]
        public void SetSanityDefaults()
        {
            var corsPolicy = new CorsPolicy();
            corsPolicy.Origins.Any().Should().BeFalse();
            corsPolicy.AllowAnyOrigin.Should().BeFalse();
            corsPolicy.Headers.Any().Should().BeFalse();
            corsPolicy.AllowAnyHeader.Should().BeFalse();
            corsPolicy.Methods.Any().Should().BeFalse();
            corsPolicy.AllowAnyMethod.Should().BeFalse();
            corsPolicy.ExposedHeaders.Should().HaveCount(0);

            corsPolicy.SetSanityDefaults();
            corsPolicy.AllowAnyOrigin.Should().BeTrue();
            corsPolicy.AllowAnyHeader.Should().BeTrue();
            corsPolicy.AllowAnyMethod.Should().BeTrue();
            corsPolicy.ExposedHeaders.Should().HaveCount(1);
            corsPolicy.ExposedHeaders.Should().Contain("*");
        }
    }
}
