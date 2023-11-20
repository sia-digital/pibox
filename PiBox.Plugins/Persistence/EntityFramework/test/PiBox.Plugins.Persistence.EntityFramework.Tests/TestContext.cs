using Microsoft.EntityFrameworkCore;
using PiBox.Plugins.Persistence.Abstractions;

namespace PiBox.Plugins.Persistence.EntityFramework.Tests
{
    internal record TestEntity(Guid Id, string Name, DateTime CreationDate) : IGuidIdentifier, ICreationDate
    {
        public Guid Id { get; set; } = Id;
        public DateTime CreationDate { get; set; } = CreationDate;
    }

    internal class TestContext : DbContext, IDbContext<TestEntity>
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbContext GetContext() => this;

        public DbSet<TestEntity> GetSet() => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).IsRequired();
                e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
