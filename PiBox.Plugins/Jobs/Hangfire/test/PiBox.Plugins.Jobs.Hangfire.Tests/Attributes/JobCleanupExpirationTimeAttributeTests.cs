using FluentAssertions;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.States;
using Hangfire.Storage;
using NSubstitute;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Attributes;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Attributes
{
    public class JobCleanupExpirationTimeAttributeTests
    {
        [Test]
        public void JobExpirationTimeoutIsAppliedCorrectly()
        {
            JobStorage.Current = new MemoryStorage();

            var filter = new JobCleanupExpirationTimeAttribute(9999);

            var job = new global::Hangfire.Common.Job(typeof(TestJobTimeoutAsync),
                typeof(TestJobTimeoutAsync).GetMethod(nameof(TestJobTimeoutAsync.ExecuteAsync)), CancellationToken.None);

            var writeOnlyTransaction = Substitute.For<IWriteOnlyTransaction>();
            var context = new ApplyStateContext(
                new MemoryStorage(),
                Substitute.For<IStorageConnection>(),
                writeOnlyTransaction,
                new BackgroundJob("id1", job, DateTime.Now),
                new ScheduledState(DateTime.Now),
                "oldState"
            );
            filter.OnStateApplied(
                context,
                writeOnlyTransaction
            );
            filter.OnStateUnapplied(context, writeOnlyTransaction);

            context.JobExpirationTimeout.Should().Be(TimeSpan.FromDays(9999));
        }
    }
}
