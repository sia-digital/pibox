using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Formatters;

namespace PiBox.Hosting.WebHost.Tests.Formatters
{
    public class YamlInputFormatterTests
    {
        private readonly YamlInputFormatter _sut = new();

        private static InputFormatterContext GetContext<T>(string body)
        {
            var httpContext = new DefaultHttpContext
            {
                Request = { Body = new MemoryStream(Encoding.Unicode.GetBytes(body)) }
            };
            var modelMetadataComp = Substitute.For<ICompositeMetadataDetailsProvider>();
            var metadataProvider = new DefaultModelMetadataProvider(modelMetadataComp);
            var metadata = metadataProvider.GetMetadataForType(typeof(T));
            var context = new InputFormatterContext(httpContext, typeof(T).Name, new ModelStateDictionary(), metadata,
                (sr, enc) => new StreamReader(sr, enc));
            return context;
        }

        private class IdClass
        {
            public int Id { get; set; }
        }

        private record IdRecord(int Id);

        [Test]
        public async Task CanDeserializeClasses()
        {
            var context = GetContext<IdClass>("id: 123");
            var result = await _sut.ReadRequestBodyAsync(context, Encoding.Unicode);
            result.Model.Should().NotBeNull();
            result.Model.Should().BeOfType<IdClass>();
            (result.Model as IdClass)!.Id.Should().Be(123);
            result.HasError.Should().BeFalse();
        }

        [Test]
        public async Task CantDeserializeRecords()
        {
            var context = GetContext<IdRecord>("id: 123");
            var result = await _sut.ReadRequestBodyAsync(context, Encoding.Unicode);
            result.Model.Should().BeNull();
            result.HasError.Should().BeTrue();
        }
    }
}
