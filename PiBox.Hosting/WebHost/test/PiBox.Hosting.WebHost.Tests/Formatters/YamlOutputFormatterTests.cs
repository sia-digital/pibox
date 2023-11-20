using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.WebHost.Formatters;

namespace PiBox.Hosting.WebHost.Tests.Formatters
{
    public class YamlOutputFormatterTests
    {
        private readonly YamlOutputFormatter _sut = new();
        private TextWriter _textWriter;

        private OutputFormatterWriteContext GetContext<T>(T obj)
        {
            _textWriter = Substitute.For<TextWriter>();
            var httpContext = new DefaultHttpContext
            {
                Response = { Body = new MemoryStream() }
            };
            var context = new OutputFormatterWriteContext(httpContext, (s, e) => _textWriter, typeof(T), obj);
            return context;
        }

        private class IdClass
        {
            public int Id { get; set; }
        }

        private record IdRecord(int Id);

        [Test]
        public async Task CanSerializeClasses()
        {
            var context = GetContext(new IdClass { Id = 123 });
            await _sut.WriteResponseBodyAsync(context, Encoding.Unicode);
            await _textWriter.Received(1).WriteAsync(Arg.Is($"id: 123{Environment.NewLine}"));
        }

        [Test]
        public async Task CanSerializeRecords()
        {
            var context = GetContext(new IdRecord(123));
            await _sut.WriteResponseBodyAsync(context, Encoding.Unicode);
            await _textWriter.Received(1).WriteAsync(Arg.Is($"id: 123{Environment.NewLine}"));
        }
    }
}
