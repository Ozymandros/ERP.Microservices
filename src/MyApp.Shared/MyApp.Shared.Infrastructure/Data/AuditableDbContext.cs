using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Shared.Infrastructure.Data;

/// <summary>
/// Provides Auditable Db Context functionality.
/// </summary>
public class AuditableDbContext : DbContext
{
    /// <summary>base.</summary>
    public AuditableDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>Save Changes Async.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is not IAuditableEntity entity)
            {
                continue;
            }

            var currentUser = ResolveCurrentUser();

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = currentUser;
            }
            else
            {
                Entry(entity).Property(p => p.CreatedAt).IsModified = false;
                Entry(entity).Property(p => p.CreatedBy).IsModified = false;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = currentUser;
        }
    }

    private string ResolveCurrentUser()
    {
        const string defaultUser = "SystemUser";

        try
        {
            var httpContextAccessor = this.GetService<IHttpContextAccessor>();
            var name = httpContextAccessor?.HttpContext?.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (Thread.CurrentPrincipal?.Identity?.IsAuthenticated == true &&
                !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                return Thread.CurrentPrincipal.Identity.Name;
            }
        }
        catch
        {
            // If resolving IHttpContextAccessor fails for any reason, keep the default SystemUser
        }

        return defaultUser;
    }
}
