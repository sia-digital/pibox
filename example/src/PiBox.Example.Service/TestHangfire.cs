using Hangfire;
using PiBox.Plugins.Jobs.Hangfire;
using PiBox.Plugins.Jobs.Hangfire.Attributes;
using PiBox.Plugins.Jobs.Hangfire.Job;

namespace PiBox.Example.Service
{
    public class TestHangfire : IHangfireConfigurator
    {
        public bool IncludesStorage => false;

        public void Configure(IGlobalConfiguration config)
        { }

        public void ConfigureServer(BackgroundJobServerOptions options)
        {
            options.WorkerCount = 1;
        }
    }

    [RecurringJob("*/1 * * * *")]
    public class TestJob : AsyncJob
    {
        public TestJob(ILogger logger) : base(logger)
        {
        }

        protected override Task<object> ExecuteJobAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(new { Hello = "World" });
        }
    }
}
