using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Temperature)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(p => p.TopK)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(p => p.MaxTokens)
            .IsRequired()
            .HasDefaultValue(2048);

        builder.Property(p => p.EmbeddingDimensions)
            .IsRequired()
            .HasDefaultValue(1536);

        builder.Property(p => p.EnableMemory)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.EnableRAG)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.EmbeddingModelName)
            .HasMaxLength(200);

        builder.Property(p => p.SystemInstructions)
            .HasMaxLength(8000);

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.BotType)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(BotType.Chat);

        builder.Property(p => p.TenantId)
            .IsRequired(false);

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.IsActive);

        builder.HasMany(p => p.Plugins)
            .WithOne(p => p.Agent)
            .HasForeignKey(p => p.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Sessions)
            .WithOne(s => s.Agent)
            .HasForeignKey(s => s.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}