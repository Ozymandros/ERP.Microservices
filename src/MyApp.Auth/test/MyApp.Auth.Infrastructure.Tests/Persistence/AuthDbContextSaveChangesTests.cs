using FluentAssertions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Tests.Helpers;
using Xunit;

namespace MyApp.Auth.Infrastructure.Tests.Persistence;

public class AuthDbContextSaveChangesTests
{
    [Fact]
    public void SaveChanges_NewPermission_StampsAuditFields()
    {
        using var ctx = TestDbContextFactory.CreateInMemoryContext();
        var permission = new Permission(Guid.NewGuid())
        {
            Module = "Orders",
            Action = "Read",
            Description = "Read orders"
        };

        ctx.Permissions.Add(permission);
        ctx.SaveChanges();

        permission.CreatedBy.Should().Be("SystemUser");
        permission.UpdatedBy.Should().Be("SystemUser");
        permission.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        permission.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SaveChanges_UpdatedPermission_PreservesCreatedAudit()
    {
        using var ctx = TestDbContextFactory.CreateInMemoryContext();
        var permission = new Permission(Guid.NewGuid())
        {
            Module = "Billing",
            Action = "Issue",
        };
        ctx.Permissions.Add(permission);
        ctx.SaveChanges();
        var createdAt = permission.CreatedAt;
        var createdBy = permission.CreatedBy;

        permission.Description = "Updated";
        ctx.SaveChanges();

        permission.CreatedAt.Should().Be(createdAt);
        permission.CreatedBy.Should().Be(createdBy);
        permission.UpdatedAt.Should().BeOnOrAfter(createdAt);
    }
}