using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Repositories;

public class EntityChangeSnapshotTests
{
    private sealed class SnapshotEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class SnapshotDbContext : DbContext
    {
        public SnapshotDbContext(DbContextOptions<SnapshotDbContext> options) : base(options) { }

        public DbSet<SnapshotEntity> Items => Set<SnapshotEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SnapshotEntity>().HasKey(e => e.Id);
        }
    }

    [Fact]
    public async Task CommitAsync_ModifiedEntity_IncludesChangedProperties()
    {
        var id = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<SnapshotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new SnapshotDbContext(options);
        context.Items.Add(new SnapshotEntity { Id = id, Name = "Old", Email = "old@test.com" });
        await context.SaveChangesAsync();

        var entity = await context.Items.FirstAsync();
        entity.Name = "New";
        entity.Email = "new@test.com";

        var changes = await EntityChangeSnapshot.CommitAsync(context);

        changes.Should().ContainSingle();
        var change = changes.Single();
        change.State.Should().Be("Modified");
        change.Properties.Should().HaveCount(2);
        change.Properties.Should().Contain(p =>
            p.PropertyName == "Name" && Equals(p.OldValue, "Old") && Equals(p.NewValue, "New"));
        change.Properties.Should().Contain(p =>
            p.PropertyName == "Email" && Equals(p.OldValue, "old@test.com") && Equals(p.NewValue, "new@test.com"));
        change.OriginalValue.Should().NotBeNullOrWhiteSpace();
        change.NewValue.Should().NotBeNullOrWhiteSpace();
    }
}
