using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Notes;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.LeadId);
        builder.HasIndex(x => x.OpportunityId);
        builder.HasIndex(x => x.ActivityId);
    }
}

