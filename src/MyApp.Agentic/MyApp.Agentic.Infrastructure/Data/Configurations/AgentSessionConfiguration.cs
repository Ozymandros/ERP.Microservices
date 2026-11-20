using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.Sessions;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

/// <summary>EF Core entity type configuration for <see cref="AgentSession"/>, mapping to the <c>AgentSessions</c> table.</summary>
public class AgentSessionConfiguration : IEntityTypeConfiguration<AgentSession>
{
    /// <summary>
    /// Configures the entity type mapping.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<AgentSession> builder)
    {
        builder.ToTable("AgentSessions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Title)
            .HasMaxLength(500);

        builder.Property(p => p.StartedAt)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(p => new { p.AgentId, p.UserId, p.Status });
    }
}