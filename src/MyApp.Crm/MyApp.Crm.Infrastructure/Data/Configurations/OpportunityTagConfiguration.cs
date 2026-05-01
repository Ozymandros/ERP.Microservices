using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Crm.Domain.Tags;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Opportunity Tag Configuration functionality.
/// </summary>
public class OpportunityTagConfiguration : IEntityTypeConfiguration<OpportunityTag>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<OpportunityTag> builder)
    {
        builder.ToTable("OpportunityTags");
        builder.HasKey(x => new { x.OpportunityId, x.TagId });

        builder.HasOne<Opportunity>()
            .WithMany(o => o.Tags)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

