using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Shared.Infrastructure.Data;

public class AuditableDbContext : DbContext
{
    public AuditableDbContext(DbContextOptions options) : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
            if (entry.Entity is IAuditableEntity)
            {
                var entity = (IAuditableEntity)entry.Entity;
                // Resolve current user name from common ambient contexts:
                // 1. Try IHttpContextAccessor from the DbContext service provider (if available)
                // 2. Fall back to Thread.CurrentPrincipal
                // 3. Final fallback to SystemUser
                string currentUser = "SystemUser";
                try
                {
                    var httpContextAccessor = this.GetService<IHttpContextAccessor>();
                    var name = httpContextAccessor?.HttpContext?.User?.Identity?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        currentUser = name;
                    }
                    else if (Thread.CurrentPrincipal?.Identity?.IsAuthenticated == true &&
                             !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
                    {
                        currentUser = Thread.CurrentPrincipal.Identity.Name;
                    }
                }
                catch
                {
                    // If resolving IHttpContextAccessor fails for any reason, keep the default SystemUser
                }
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

        return await base.SaveChangesAsync(cancellationToken);
    }
}
