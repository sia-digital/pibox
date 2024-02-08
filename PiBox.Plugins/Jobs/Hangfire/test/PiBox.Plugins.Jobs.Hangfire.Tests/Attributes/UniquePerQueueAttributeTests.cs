using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Attributes
{
    public class UniquePerQueueAttributeTests
    {
        private const string Queue = "test";
        private ElectStateContext _context = null!;
        private IMonitoringApi _monitoringApi = null!;
        private UniquePerQueueAttribute _attribute = null!;

        private void Setup(BackgroundJob backgroundJob, bool includeProcessingJobs = false, bool includeScheduledJobs = false)
        {
            var storage = Substitute.For<JobStorage>();
            var con = Substitute.For<IStorageConnection>();
            var state = Substitute.For<IState>();
            var transaction = Substitute.For<IWriteOnlyTransaction>();
            var applyStateContext = new ApplyStateContext(
                storage: storage,
                connection: con,
                transaction: transaction,
                backgroundJob: backgroundJob,
                newState: state,
                oldStateName: null);
            _context = Substitute.For<ElectStateContext>(applyStateContext);
            _context.CandidateState = new EnqueuedState();
            _monitoringApi = Substitute.For<IMonitoringApi>();
            _context.Storage.GetMonitoringApi().Returns(_monitoringApi);
            _context.BackgroundJob.Returns(backgroundJob);
            _attribute = new UniquePerQueueAttribute(Queue)
            {
                CheckRunningJobs = includeProcessingJobs,
                CheckScheduledJobs = includeScheduledJobs
            };
        }

        [Test]
        public void DoesNotRemoveItself()
        {
            var job = CreateJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var enqueuedJob = new EnqueuedJobDto { Job = job };
            Setup(backgroundJob);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("1", enqueuedJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<EnqueuedState>();
            var state = _context.CandidateState as EnqueuedState;
            state!.Queue.Should().Be(Queue);
        }

        [Test]
        public void RemovesTheDuplicateFromEnqueuedOnes()
        {
            var job = CreateJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var enqueuedJob = new EnqueuedJobDto { Job = job };
            Setup(backgroundJob);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", enqueuedJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void RemovesTheDuplicateFromProcessingOnes()
        {
            var job = CreateJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var processingJob = new ProcessingJobDto { Job = job };
            Setup(backgroundJob, true);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList<EnqueuedJobDto>());
            _monitoringApi.ProcessingJobs(Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", processingJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void RemovesTheDuplicateFromScheduledOnes()
        {
            var job = CreateJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var scheduledJob = new ScheduledJobDto { Job = job };
            Setup(backgroundJob, false, true);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList<EnqueuedJobDto>());
            _monitoringApi.ScheduledJobs(Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", scheduledJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void DoesNothingOnWrongState()
        {
            var job = CreateJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            Setup(backgroundJob, false, true);
            var state = new ScheduledState(TimeSpan.FromMilliseconds(100));
            _context.CandidateState = state;
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<ScheduledState>();
            _context.CandidateState.Should().Be(state);
        }

        [Test]
        public void ParameterizedJobDoesNotRemoveItself()
        {
            var job = CreateParameterizedJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var enqueuedJob = new EnqueuedJobDto { Job = job };
            Setup(backgroundJob);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("1", enqueuedJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<EnqueuedState>();
            var state = _context.CandidateState as EnqueuedState;
            state!.Queue.Should().Be(Queue);
        }

        [Test]
        public void ParameterizedJobRemovesTheDuplicateFromEnqueuedOnes()
        {
            var job = CreateParameterizedJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var enqueuedJob = new EnqueuedJobDto { Job = job };
            Setup(backgroundJob);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", enqueuedJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void ParameterizedJobRemovesTheDuplicateFromProcessingOnes()
        {
            var job = CreateParameterizedJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var processingJob = new ProcessingJobDto { Job = job };
            Setup(backgroundJob, true);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList<EnqueuedJobDto>());
            _monitoringApi.ProcessingJobs(Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", processingJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void ParameterizedJobRemovesTheDuplicateFromScheduledOnes()
        {
            var job = CreateParameterizedJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            var scheduledJob = new ScheduledJobDto { Job = job };
            Setup(backgroundJob, false, true);
            _monitoringApi.EnqueuedJobs(Queue, Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList<EnqueuedJobDto>());
            _monitoringApi.ScheduledJobs(Arg.Any<int>(), Arg.Any<int>())
                .Returns(GetJobList(("2", scheduledJob)));
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<DeletedState>();
            var state = _context.CandidateState as DeletedState;
            state!.Reason.Should().Be("Instance of the same job is already queued.");
        }

        [Test]
        public void ParameterizedJobDoesNothingOnWrongState()
        {
            var job = CreateParameterizedJob();
            var backgroundJob = new BackgroundJob("1", job, DateTime.Now);
            Setup(backgroundJob, false, true);
            var state = new ScheduledState(TimeSpan.FromMilliseconds(100));
            _context.CandidateState = state;
            _attribute.OnStateElection(_context);
            _context.CandidateState.Should().BeOfType<ScheduledState>();
            _context.CandidateState.Should().Be(state);
        }

        private static JobList<T> GetJobList<T>(params (string, T)[] jobs)
        {
            var entries = jobs.Select(x => new KeyValuePair<string, T>(x.Item1, x.Item2));
            return new JobList<T>(entries);
        }

        private static global::Hangfire.Common.Job CreateJob()
        {
            var job = new global::Hangfire.Common.Job(typeof(TestJob),
                typeof(TestJob).GetMethod(nameof(TestJob.ExecuteAsync)), CancellationToken.None);
            return job;
        }

        private static global::Hangfire.Common.Job CreateParameterizedJob()
        {
            var job = new global::Hangfire.Common.Job(typeof(ParameterizedTestJob),
                typeof(ParameterizedTestJob).GetMethod(nameof(ParameterizedTestJob.ExecuteAsync)), new TestJobPayload(), CancellationToken.None);
            return job;
        }

        [ExcludeFromCodeCoverage]
        private class TestJob : IAsyncJob
        {

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Cleanup
            }

            public Task<object> ExecuteAsync(CancellationToken jobCancellationToken)
            {
                object x = new { success = true };
                return Task.FromResult(x);
            }
        }

        [ExcludeFromCodeCoverage]
        private class ParameterizedTestJob : IParameterizedAsyncJob<TestJobPayload>
        {
            public Task<object> ExecuteAsync(TestJobPayload value, CancellationToken jobCancellationToken)
            {
                object x = new { success = true };
                return Task.FromResult(x);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Cleanup
            }
        }

        [ExcludeFromCodeCoverage]
        private class TestJobPayload
        {

        }
    }
}
