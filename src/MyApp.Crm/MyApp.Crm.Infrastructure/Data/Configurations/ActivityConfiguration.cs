using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Activities;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AssignedToUsername).HasMaxLength(256).IsRequired();

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(x => x.DueAt).IsRequired();

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(n => n.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Single-parent rule enforced in domain, but also keep queryable indices
        builder.HasIndex(x => x.LeadId);
        builder.HasIndex(x => x.OpportunityId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.AssignedToUsername, x.DueAt });
    }
}

