using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class JobTest
    {
        private readonly ILogger _logger = Substitute.For<ILogger>();

        [Test]
        public async Task AsyncJobTest()
        {
            var job = new TestJobSuccessAsync(_logger);
            var result = await job.ExecuteAsync(CancellationToken.None);
            result.Should().Be("test");
        }

        [Test]
        public void AsyncJobWillBeCancelledAfterTimeout()
        {
            var job = new TestJobTimeoutAsync(_logger);
            job.Invoking(async x => await x.ExecuteAsync(new CancellationToken(true))).Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public void AsyncJobThrowsException()
        {
            var job = new JobFailsJob(_logger);
            job.Invoking(async x => await x.ExecuteAsync(new CancellationToken(true))).Should()
                .ThrowAsync<NotSupportedException>();
        }

        [Test]
        public async Task ParameterizedAsyncJobTest()
        {
            var param = "Test";
            var job = new ParameterizedAsyncJobTest(_logger);
            var result = await job.ExecuteAsync(param, CancellationToken.None);
            result.Should().Be(param);
        }

        [Test]
        public void ParameterizedAsyncJobWillBeCancelledAfterTimeout()
        {
            var param = "Test";
            var job = new ParameterizedAsyncJobTest(_logger);
            job.Invoking(async x => await x.ExecuteAsync(param, new CancellationToken(true))).Should()
                .ThrowAsync<OperationCanceledException>();
        }
    }
}
