using FluentAssertions;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Extensions;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Extensionms
{
    public class MonitoringApiExtensionsTests
    {
        private IMonitoringApi _api;

        [SetUp]
        public void Up()
        {
            _api = Substitute.For<IMonitoringApi>();
        }

        private static JobList<FetchedJobDto> FakeJobList(int count)
        {
            var jobs = new FetchedJobDto[count];
            for (var i = 0; i < count; i++)
                jobs[i] = new FetchedJobDto();
            var dic = jobs.Select(x => new KeyValuePair<string, FetchedJobDto>(Guid.NewGuid().ToString(), x));
            return new JobList<FetchedJobDto>(dic);
        }

        [Test]
        public void PollsThroughPagination()
        {
            var first = FakeJobList(500);
            var second = FakeJobList(499);
            _api.FetchedJobs(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(first, second);

            var result = _api.GetCompleteList((api, page) => api.FetchedJobs("default", page.Offset, page.PageSize));
            result.Count.Should().Be(first.Count + second.Count);
            var keys = result.Select(x => x.Key).ToArray();
            foreach (var mockData in first.UnionBy(second, x => x.Key))
            {
                keys.Should().Contain(mockData.Key);
            }
        }
    }
}
