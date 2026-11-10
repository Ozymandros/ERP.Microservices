using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Projects.Domain.Entities;

namespace MyApp.Projects.Infrastructure.Data.Configurations;

/// <summary>EF configuration for <see cref="Project"/>.</summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProjectNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.ProjectNumber).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.Blocked).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(100);

        builder.HasMany(x => x.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF configuration for <see cref="ProjectTask"/>.</summary>
public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskNo).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.TaskNo }).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.ParentTaskId);

        // Self-referencing tree. Restrict: children must be moved/deleted explicitly (no cascade cycles).
        builder.HasOne(x => x.ParentTask)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF configuration for <see cref="ProjectPlanningLine"/>.</summary>
public class ProjectPlanningLineConfiguration : IEntityTypeConfiguration<ProjectPlanningLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectPlanningLine> builder)
    {
        builder.ToTable("ProjectPlanningLines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.LineNo }).IsUnique();
        builder.HasIndex(x => x.ProjectTaskId);
        builder.Property(x => x.LineType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(20);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitCostLcy).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.LineDiscountPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ExchangeRate).HasColumnType("decimal(18,8)");
        builder.Property(x => x.TotalCostLcy).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineAmountLcy).HasColumnType("decimal(18,2)");
        builder.Property(x => x.QuantityPosted).HasColumnType("decimal(18,4)");
        builder.Property(x => x.QuantityInConsumption).HasColumnType("decimal(18,4)");
        builder.Property(x => x.QuantityTransferredToInvoice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.QuantityInvoiced).HasColumnType("decimal(18,4)");

        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(x => x.ProjectTaskId).OnDelete(DeleteBehavior.NoAction);

        builder.Ignore(x => x.IsBudget);
        builder.Ignore(x => x.IsBillable);
        builder.Ignore(x => x.QuantityToInvoice);
        builder.Ignore(x => x.RemainingQuantity);
        builder.Ignore(x => x.NetUnitPrice);
    }
}

/// <summary>EF configuration for <see cref="ProjectLedgerEntry"/>.</summary>
public class ProjectLedgerEntryConfiguration : IEntityTypeConfiguration<ProjectLedgerEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectLedgerEntry> builder)
    {
        builder.ToTable("ProjectLedgerEntries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.EntryNo }).IsUnique();
        builder.HasIndex(x => x.ProjectTaskId);
        builder.HasIndex(x => x.PlanningLineId);
        builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SourceDocument).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SourceDocumentNo).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitCostLcy).HasColumnType("decimal(18,4)");
        builder.Property(x => x.TotalCostLcy).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineAmountLcy).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExchangeRate).HasColumnType("decimal(18,8)");

        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProjectTask>().WithMany().HasForeignKey(x => x.ProjectTaskId).OnDelete(DeleteBehavior.NoAction);
    }
}

/// <summary>EF configuration for <see cref="ProjectInvoiceRequest"/> and its lines.</summary>
public class ProjectInvoiceRequestConfiguration :
    IEntityTypeConfiguration<ProjectInvoiceRequest>,
    IEntityTypeConfiguration<ProjectInvoiceRequestLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectInvoiceRequest> builder)
    {
        builder.ToTable("ProjectInvoiceRequests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.InvoiceId);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectInvoiceRequestLine> builder)
    {
        builder.ToTable("ProjectInvoiceRequestLines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PlanningLineId);
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.TaxRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");
    }
}

/// <summary>EF configuration for <see cref="ProjectConsumptionOrder"/> and its lines.</summary>
public class ProjectConsumptionOrderConfiguration :
    IEntityTypeConfiguration<ProjectConsumptionOrder>,
    IEntityTypeConfiguration<ProjectConsumptionOrderLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectConsumptionOrder> builder)
    {
        builder.ToTable("ProjectConsumptionOrders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.ProjectId);
        builder.Property(x => x.OrderNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.ConsumptionOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectConsumptionOrderLine> builder)
    {
        builder.ToTable("ProjectConsumptionOrderLines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PlanningLineId);
    }
}

/// <summary>EF configuration for <see cref="Resource"/> and <see cref="ResourceGroup"/>.</summary>
public class ResourceConfiguration :
    IEntityTypeConfiguration<Resource>,
    IEntityTypeConfiguration<ResourceGroup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceNo).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.ResourceNo).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BaseUnitOfMeasure).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.DailyCapacity).HasColumnType("decimal(9,2)");
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.ResourceGroup)
            .WithMany()
            .HasForeignKey(x => x.ResourceGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ResourceGroup> builder)
    {
        builder.ToTable("ResourceGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}

/// <summary>EF configuration for <see cref="Currency"/> and <see cref="ExchangeRate"/>.</summary>
public class CurrencyConfiguration :
    IEntityTypeConfiguration<Currency>,
    IEntityTypeConfiguration<ExchangeRate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(3).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(5);

        builder.HasMany(x => x.ExchangeRates)
            .WithOne(r => r.Currency)
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CurrencyId, x.StartingDate }).IsUnique();
        builder.Property(x => x.RateToLocal).HasColumnType("decimal(18,8)");
    }
}
