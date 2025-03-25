using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests
{
    [SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method")]
    public class JobManagerTests
    {
        private static readonly global::Hangfire.Common.Job _testJob = new(typeof(TestJob), typeof(TestJob).GetMethod("ExecuteAsync"), new List<object> { CancellationToken.None });
        private IRecurringJobManager _recurringJobManager;
        private IBackgroundJobClient _backgroundJobClient;
        private IStorageConnection _storageConnection;
        private IMonitoringApi _monitoringApi;
        private JobManager GetJobManager(bool hasQueueSupport = false) =>
            new(hasQueueSupport, _storageConnection, _monitoringApi, _recurringJobManager, _backgroundJobClient);

        [SetUp]
        public void Up()
        {
            _recurringJobManager = Substitute.For<IRecurringJobManager>();
            _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
            _storageConnection = Substitute.For<IStorageConnection>();
            _monitoringApi = Substitute.For<IMonitoringApi>();
        }

        [Test]
        public void GetQueuesWorks()
        {
            _monitoringApi.Queues().Returns([new QueueWithTopEnqueuedJobsDto { Name = "test" }]);
            var result = GetJobManager().GetQueues();
            result.Should().HaveCount(1);
            result.Should().Contain("test");
        }

        [Test]
        public void GetRecurringJobsWorks()
        {
            _storageConnection.GetAllItemsFromSet("recurring-jobs")
                .Returns(["test"]);
            _storageConnection.GetAllEntriesFromHash("recurring-job:test")
                .Returns(new Dictionary<string, string>
                {
                    { "Cron", "* * * * *" },
                    { "Job", new { t = TypeHelper.CurrentTypeSerializer(typeof(TestJob)), m = nameof(TestJob.ExecuteAsync), p = new List<string> {TypeHelper.CurrentTypeSerializer(typeof(CancellationToken))}, a = new string[] {null} }.Serialize() }
                });
            var result = GetJobManager().GetRecurringJobs();
            result.Should().HaveCount(1);
            result.Should().Contain(x => x.Id == "test");

            result = GetJobManager().GetRecurringJobs<TestJob>();
            result.Should().HaveCount(1);
            result.Should().Contain(x => x.Id == "test");
            result = GetJobManager().GetRecurringJobs<TestJob>(x => x.Id == "test");
            result.Should().HaveCount(1);
            result.Should().Contain(x => x.Id == "test");
            result = GetJobManager().GetRecurringJobs<TestJob>(x => x.Id != "test");
            result.Should().HaveCount(0);
        }
        [Test]
        public void GetEnqueuedJobsWorks()
        {
            var dto = new EnqueuedJobDto { Job = _testJob };
            _monitoringApi.Queues().Returns([new QueueWithTopEnqueuedJobsDto { Name = "test" }]);
            _monitoringApi.EnqueuedJobs("test", 0, 500)
                .Returns(new JobList<EnqueuedJobDto>(new List<KeyValuePair<string, EnqueuedJobDto>>
                {
                    new("test1", dto)
                }));
            var result = GetJobManager().GetEnqueuedJobs();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetEnqueuedJobs<TestJob>();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetEnqueuedJobs<TestJob>(x => x.Job.Method.Name == "ExecuteAsync");
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetEnqueuedJobs<TestJob>(x => x.Job.Method.Name == "Run");
            result.Should().HaveCount(0);
        }

        [Test]
        public void GetProcessingJobsWorks()
        {
            var dto = new ProcessingJobDto { Job = _testJob };
            _monitoringApi.ProcessingJobs(0, 500)
                .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>
                {
                    new("test1", dto)
                }));
            var result = GetJobManager().GetProcessingJobs();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetProcessingJobs<TestJob>();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetProcessingJobs<TestJob>(x => x.Job.Method.Name == "ExecuteAsync");
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetProcessingJobs<TestJob>(x => x.Job.Method.Name == "Run");
            result.Should().HaveCount(0);
        }

        [Test]
        public void GetFailedJobsWorks()
        {
            var dto = new FailedJobDto { Job = _testJob };
            _monitoringApi.FailedJobs(0, 500)
                .Returns(new JobList<FailedJobDto>(new List<KeyValuePair<string, FailedJobDto>>
                {
                    new("test1", dto)
                }));
            var result = GetJobManager().GetFailedJobs();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFailedJobs<TestJob>();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFailedJobs<TestJob>(x => x.Job.Method.Name == "ExecuteAsync");
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFailedJobs<TestJob>(x => x.Job.Method.Name == "Run");
            result.Should().HaveCount(0);
        }

        [Test]
        public void GetFetchedJobsWorks()
        {
            var dto = new FetchedJobDto { Job = _testJob };
            _monitoringApi.Queues().Returns([new QueueWithTopEnqueuedJobsDto { Name = "test" }]);
            _monitoringApi.FetchedJobs("test", 0, 500)
                .Returns(new JobList<FetchedJobDto>(new List<KeyValuePair<string, FetchedJobDto>>
                {
                    new("test1", dto)
                }));
            var result = GetJobManager().GetFetchedJobs();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFetchedJobs<TestJob>();
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFetchedJobs<TestJob>(x => x.Job.Method.Name == "ExecuteAsync");
            result.Should().HaveCount(1);
            result.Should().Contain(dto);

            result = GetJobManager().GetFetchedJobs<TestJob>(x => x.Job.Method.Name == "Run");
            result.Should().HaveCount(0);
        }

        [Test]
        public void GetJobsWorks()
        {
            _monitoringApi.Queues().Returns([new QueueWithTopEnqueuedJobsDto { Name = "test" }]);
            _monitoringApi.FetchedJobs("test", 0, 500)
                .Returns(new JobList<FetchedJobDto>(new List<KeyValuePair<string, FetchedJobDto>>
                {
                    new("test1", new FetchedJobDto {Job = _testJob})
                }));
            _monitoringApi.FailedJobs(0, 500)
                .Returns(new JobList<FailedJobDto>(new List<KeyValuePair<string, FailedJobDto>>
                {
                    new("test1", new FailedJobDto {Job = _testJob})
                }));
            _monitoringApi.ProcessingJobs(0, 500)
                .Returns(new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>
                {
                    new("testProc", new ProcessingJobDto {Job = _testJob})
                }));
            _monitoringApi.EnqueuedJobs("test", 0, 500)
                .Returns(new JobList<EnqueuedJobDto>(new List<KeyValuePair<string, EnqueuedJobDto>>
                {
                    new("testEnq", new EnqueuedJobDto {Job = _testJob})
                }));
            _storageConnection.GetAllItemsFromSet("recurring-jobs")
                .Returns(["test"]);
            _storageConnection.GetAllEntriesFromHash("recurring-job:test")
                .Returns(new Dictionary<string, string>
                {
                    { "Cron", "* * * * *" },
                    { "Job", new { t = TypeHelper.CurrentTypeSerializer(typeof(TestJob)), m = nameof(TestJob.ExecuteAsync), p = new List<string> {TypeHelper.CurrentTypeSerializer(typeof(CancellationToken))}, a = new string[] {null} }.Serialize() }
                });
            var result = GetJobManager().GetJobs();
            result.Should().HaveCount(Enum.GetValues(typeof(JobType)).Length);
            foreach (var val in Enum.GetValues(typeof(JobType)).OfType<JobType>())
            {
                result.Should().Contain(x => x.Type == val && x.Job.Type == typeof(TestJob) && x.JobDto != null);
            }
            result = GetJobManager().GetJobs(x => x.Type == JobType.Enqueued);
            result.Should().HaveCount(1);
            result[0].Type.Should().Be(JobType.Enqueued);
            result[0].JobDto.Should().BeOfType<EnqueuedJobDto>();
            result[0].Job.Should().Be(_testJob);
        }

        [Test]
        public void EnqueueWorks()
        {
            GetJobManager().Enqueue<TestJob>();
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob)),
                Arg.Any<EnqueuedState>());

            GetJobManager(true).Enqueue<TestJob>("test");
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob) && j.Queue == "test"),
                Arg.Any<EnqueuedState>());

            GetJobManager().Enqueue<TestParamJob, string>("123");
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob)
                    && j.Args != null && j.Args[0].ToString() == "123"),
                Arg.Any<EnqueuedState>());

            GetJobManager(true).Enqueue<TestParamJob, string>("123", "test");
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob)
                    && j.Args != null && j.Args[0].ToString() == "123" && j.Queue == "test"),
                Arg.Any<EnqueuedState>());
        }

        [Test]
        public void ScheduleWorks()
        {
            var ts = TimeSpan.FromDays(1);
            var date = DateTime.UtcNow.Add(ts).Date;
            GetJobManager().Schedule<TestJob>(ts);
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob)),
                Arg.Is<ScheduledState>(s => s.EnqueueAt.Date.Equals(date.Date)));

            GetJobManager(true).Schedule<TestJob>(ts, "test");
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob) && j.Queue == "test"),
                Arg.Is<ScheduledState>(s => s.EnqueueAt.Date.Equals(date.Date)));

            GetJobManager().Schedule<TestParamJob, string>("123", ts);
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob)
                    && j.Args != null && j.Args[0].ToString() == "123"),
                Arg.Is<ScheduledState>(s => s.EnqueueAt.Date.Equals(date.Date)));

            GetJobManager(true).Schedule<TestParamJob, string>("123", ts, "test");
            _backgroundJobClient.Received(1).Create(Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob)
                    && j.Args != null && j.Args[0].ToString() == "123" && j.Queue == "test"),
                Arg.Is<ScheduledState>(s => s.EnqueueAt.Date.Equals(date.Date)));
        }

        [Test]
        public void RegisterRecurringWorks()
        {
            const string Cron = "* * * * *";
            GetJobManager().RegisterRecurring<TestJob>(Cron, jobSuffix: "suffix");
            _recurringJobManager.Received(1).AddOrUpdate(
                Arg.Is<string>(s => s.Contains("Test") && s.Contains("suffix")),
                Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob)),
                Cron,
                Arg.Is<RecurringJobOptions>(r => r.TimeZone == TimeZoneInfo.Utc)
                );

            GetJobManager(true).RegisterRecurring<TestJob>(Cron, "test", "suffix");
            _recurringJobManager.Received(1).AddOrUpdate(
                Arg.Is<string>(s => s.Contains("Test") && s.Contains("suffix")),
                Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestJob) && j.Queue == "test"),
                Cron,
                Arg.Is<RecurringJobOptions>(r => r.TimeZone == TimeZoneInfo.Utc)
            );

            GetJobManager().RegisterRecurring<TestParamJob, string>("123", Cron, jobSuffix: "suffix");
            _recurringJobManager.Received(1).AddOrUpdate(
                Arg.Is<string>(s => s.Contains("TestParam") && s.Contains("suffix")),
                Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob) && j.Args != null && j.Args[0].ToString() == "123"),
                Cron,
                Arg.Is<RecurringJobOptions>(r => r.TimeZone == TimeZoneInfo.Utc)
            );

            GetJobManager(true).RegisterRecurring<TestParamJob, string>("123", Cron, "test", "suffix");
            _recurringJobManager.Received(1).AddOrUpdate(
                Arg.Is<string>(s => s.Contains("TestParam") && s.Contains("suffix")),
                Arg.Is<global::Hangfire.Common.Job>(j => j.Type == typeof(TestParamJob) && j.Args != null && j.Args[0].ToString() == "123" && j.Queue == "test"),
                Cron,
                Arg.Is<RecurringJobOptions>(r => r.TimeZone == TimeZoneInfo.Utc)
            );
        }

        [Test]
        public void DeleteWorks()
        {
            GetJobManager().Delete("123");
            _backgroundJobClient.Received(1).ChangeState("123", Arg.Any<DeletedState>(), Arg.Any<string>());
        }

        [Test]
        public void DeleteRecurringWorks()
        {
            GetJobManager().DeleteRecurring("123");
            _recurringJobManager.Received(1).RemoveIfExists("123");
        }
    }

    public class TestJob : AsyncJob
    {
        public TestJob(ILogger logger) : base(logger) { }
        protected override Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(new { test = true });
        }
    }

    public class TestParamJob : ParameterizedAsyncJob<string>
    {
        public TestParamJob(ILogger logger) : base(logger) { }
        protected override Task<object> ExecuteJobAsync(string value, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(new { test = true, Val = value });
        }
    }
}
