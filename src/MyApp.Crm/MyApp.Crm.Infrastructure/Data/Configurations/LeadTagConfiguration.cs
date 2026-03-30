using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Tags;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

public class LeadTagConfiguration : IEntityTypeConfiguration<LeadTag>
{
    public void Configure(EntityTypeBuilder<LeadTag> builder)
    {
        builder.ToTable("LeadTags");
        builder.HasKey(x => new { x.LeadId, x.TagId });

        builder.HasOne<Lead>()
            .WithMany(l => l.Tags)
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

