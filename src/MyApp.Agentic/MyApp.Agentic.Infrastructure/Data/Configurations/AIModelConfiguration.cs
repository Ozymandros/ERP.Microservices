using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.AIModels;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("AIModels");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TechnicalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.TokenLimit)
            .IsRequired();

        builder.Property(p => p.Capabilities)
            .HasMaxLength(1000);

        builder.HasMany(p => p.Agents)
            .WithOne(a => a.Model)
            .HasForeignKey(a => a.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}