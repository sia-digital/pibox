using FluentAssertions;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Testing.Assertions;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Attributes
{

    public class LogJobExecutionFilterTests
    {
        [Test]
        public void JobIsNotCancelledWhenMatchingFeatureIsEnabled()
        {
            JobStorage.Current = new MemoryStorage();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var fakeLogger = new FakeLogger<TestJobAsync>();
            loggerFactory.CreateLogger(default!).ReturnsForAnyArgs(fakeLogger);
            var filter = new LogJobExecutionFilter(loggerFactory);

            var job = new global::Hangfire.Common.Job(typeof(TestJobAsync),
                typeof(TestJobAsync).GetMethod(nameof(TestJobAsync.ExecuteAsync)), CancellationToken.None);
            var performContext = new PerformContext(JobStorage.Current,
                Substitute.For<IStorageConnection>(),
                new BackgroundJob("id1", job, DateTime.Now),
                new JobCancellationToken(false)
            );
            var context = new PerformingContext(
                performContext
            );
            filter.OnPerforming(
                context
            );

            fakeLogger.Entries.Should().Contain(x => x.Message == "Job with id id1 started executing");

            filter.OnPerformed(new PerformedContext(performContext, null, false, null));

            fakeLogger.Entries.Should().Contain(x => x.Message == "Job with id id1 finished executing");
        }
    }

    public class EnabledByFeatureFilterTests
    {
        [Test]
        public void JobIsNotCancelledWhenMatchingFeatureIsEnabled()
        {
            JobStorage.Current = new MemoryStorage();
            var featureManager = Substitute.For<IFeatureManager>();
            featureManager.IsEnabledAsync(Arg.Is<string>(x => x == nameof(TestJobAsync))).Returns(true);
            var filter = new EnabledByFeatureFilter(featureManager,
                new FakeLogger<EnabledByFeatureFilter>());

            var job = new global::Hangfire.Common.Job(typeof(TestJobAsync),
                typeof(TestJobAsync).GetMethod(nameof(TestJobAsync.ExecuteAsync)), CancellationToken.None);
            var context = new PerformingContext(
                new PerformContext(JobStorage.Current,
                    Substitute.For<IStorageConnection>(),
                    new BackgroundJob("id1", job, DateTime.Now),
                    new JobCancellationToken(false)
                )
            );
            filter.OnPerforming(
                context
            );

            context.Canceled.Should().BeFalse();
        }

        [Test]
        public void JobIsCancelledWhenMatchingFeatureIsDisabled()
        {
            JobStorage.Current = new MemoryStorage();
            var featureManager = Substitute.For<IFeatureManager>();
            featureManager.IsEnabledAsync(Arg.Is<string>(x => x == nameof(TestJobAsync))).Returns(false);
            var filter = new EnabledByFeatureFilter(featureManager,
                new FakeLogger<EnabledByFeatureFilter>());

            var job = new global::Hangfire.Common.Job(typeof(TestJobAsync),
                typeof(TestJobAsync).GetMethod(nameof(TestJobAsync.ExecuteAsync)), CancellationToken.None);
            var context = new PerformingContext(
                new PerformContext(JobStorage.Current,
                    Substitute.For<IStorageConnection>(),
                    new BackgroundJob("id1", job, DateTime.Now),
                    new JobCancellationToken(false)
                )
            );
            filter.OnPerforming(
                context
            );

            context.Canceled.Should().BeTrue();
        }

        [Test]
        public void JobIsCancelledWhenThereIsNoMatchingFeature()
        {
            JobStorage.Current = new MemoryStorage();
            var featureManager = Substitute.For<IFeatureManager>();
            featureManager.IsEnabledAsync(Arg.Is<string>(x => x == "asdf")).Returns(true);
            var filter = new EnabledByFeatureFilter(featureManager,
                new FakeLogger<EnabledByFeatureFilter>());

            var job = new global::Hangfire.Common.Job(typeof(TestJobAsync),
                typeof(TestJobAsync).GetMethod(nameof(TestJobAsync.ExecuteAsync)), CancellationToken.None);
            var performContext = new PerformContext(JobStorage.Current,
                Substitute.For<IStorageConnection>(),
                new BackgroundJob("id1", job, DateTime.Now),
                new JobCancellationToken(false)
            );
            var context = new PerformingContext(
                performContext
            );
            filter.OnPerforming(
                context
            );
            filter.OnPerformed(new PerformedContext(performContext, null, false, null));

            context.Canceled.Should().BeTrue();
        }
    }
}
