using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace PiBox.Plugins.Jobs.Hangfire.Attributes
{
    public class JobCleanupExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly int _cleanUpAfterDays;

        public JobCleanupExpirationTimeAttribute(int cleanUpAfterDays)
        {
            _cleanUpAfterDays = cleanUpAfterDays;
            Order = 100;
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            context.JobExpirationTimeout = TimeSpan.FromDays(_cleanUpAfterDays);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // nothing to do here
        }
    }
}
