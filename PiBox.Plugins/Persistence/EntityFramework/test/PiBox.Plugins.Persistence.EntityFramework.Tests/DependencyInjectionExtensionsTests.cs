using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NUnit.Framework;
using PiBox.Hosting.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework.Tests
{
    public class DependencyInjectionExtensionsTests
    {
        [Test]
        public void CanRegisterDbContextAndMapItsEntities()
        {
            var sc = new ServiceCollection();
            sc.AddEfContext<TestContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var sp = sc.BuildServiceProvider();
            var dbContext = sp.GetRequiredService<IDbContext<TestEntity>>();
            dbContext.Should().BeOfType<TestContext>();
            var generalDbContext = sp.GetServices<IDbContext>();
            generalDbContext.Should().HaveCount(1);
        }

        [Test]
        public void CanAddDbContextHealthCheck()
        {
            var hc = Substitute.For<IHealthChecksBuilder>();
            hc.AddEfContext<TestContext>();
            hc.Received().Add(Arg.Is<HealthCheckRegistration>(h =>
                h.Name == nameof(TestContext) && h.Tags.Contains(HealthCheckTag.Readiness.Value)));
        }

        [Test]
        public void DoesNotThrowExceptionWhenDbIsNotRelational()
        {
            var opts = new DbContextOptionsBuilder<TestContext>();
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            IDbContext context = new TestContext(opts.Options);
            context.Invoking(x => x.Migrate()).Should().NotThrow();
        }

        [Test]
        public void MigratesRelationalDatabases()
        {
            var opts = new DbContextOptionsBuilder<TestContext>();
            opts.UseSqlite($"Data Source=./test-${Guid.NewGuid():N}.db");
            IDbContext context = new TestContext(opts.Options);
            context.Invoking(x => x.Migrate()).Should().NotThrow();
        }

        [Test]
        public void CanMigrateAllContexts()
        {
            var sp = Substitute.For<IServiceProvider>();
            var dbContext = Substitute.For<IDbContext>();
            sp.GetService(typeof(IEnumerable<IDbContext>)).Returns(new List<IDbContext> { dbContext });
            var appBuilder = Substitute.For<IApplicationBuilder>();
            appBuilder.ApplicationServices.Returns(sp);
            appBuilder.MigrateEfContexts();
            appBuilder.ApplicationServices.Received(1).GetService(typeof(IEnumerable<IDbContext>));
            dbContext.Received(1).Migrate();
        }
    }
}
