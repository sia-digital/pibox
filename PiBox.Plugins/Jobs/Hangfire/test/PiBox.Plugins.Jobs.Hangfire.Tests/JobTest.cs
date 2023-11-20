using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    public class JobTest
    {
        private readonly ILogger _logger = Substitute.For<ILogger>();

        [Test]
        public async Task AsyncJobTest()
        {
            var job = new TestJobAsync(_logger);
            var result = await job.ExecuteAsync(CancellationToken.None);
            result.Should().Be("test");
        }

        [Test]
        public void AsyncJobWillBeCancelledAfterTimeout()
        {
            var job = new TestJobAsync(_logger);
            job.JobOptionsCollection = new JobDetailCollection();
            job.JobOptionsCollection.Add(new JobDetails
            {
                JobType = typeof(TestJobAsync),
                Name = "Test",
                Timeout = TimeSpan.FromMilliseconds(50)
            });
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
            job.JobOptionsCollection = new JobDetailCollection();
            job.JobOptionsCollection.Add(new JobDetails
            {
                JobType = typeof(ParameterizedAsyncJobTest),
                JobParameter = param,
                Name = "Test",
                Timeout = TimeSpan.FromMilliseconds(50)
            });
            job.Invoking(async x => await x.ExecuteAsync(param, new CancellationToken(true))).Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public void JobOptionTest()
        {
            var name = "Test";
            var jobType = typeof(string);
            var cronExpression = "expression";
            var timeout = new TimeSpan();
            var timeZoneInfo = TimeZoneInfo.Local;
            object jobParameter = "param";

            var options = new JobDetails();
            options.Name = name;
            options.JobType = jobType;
            options.CronExpression = cronExpression;
            options.Timeout = timeout;
            options.TimeZoneInfo = timeZoneInfo;
            options.JobParameter = jobParameter;

            options.Name.Should().Be(name);
            options.JobType.Should().Be(jobType);
            options.CronExpression.Should().Be(cronExpression);
            options.Timeout.Should().Be(timeout);
            options.TimeZoneInfo.Should().Be(timeZoneInfo);
            options.JobParameter.Should().Be(jobParameter);
        }
    }
}
