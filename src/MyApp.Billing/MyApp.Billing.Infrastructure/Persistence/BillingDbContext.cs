using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Billing service
/// </summary>
public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Invoice aggregate
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CustomerId)
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.TotalNet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalGross).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OutstandingAmount).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);

            // Navigation configurations
            entity.HasMany(e => e.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CreditNotes)
                .WithOne(cn => cn.OriginalInvoice)
                .HasForeignKey(cn => cn.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure InvoiceLine
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("InvoiceLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.LineNet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineGross).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.InvoiceId);
        });

        // Configure Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Method).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(e => e.ExternalPaymentId).HasMaxLength(100);

            entity.HasIndex(e => e.InvoiceId);
        });

        // Configure CreditNote
        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.ToTable("CreditNotes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(e => e.TotalNet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalGross).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.OriginalInvoiceId);

            entity.HasMany(e => e.Lines)
                .WithOne(l => l.CreditNote)
                .HasForeignKey(l => l.CreditNoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CreditNoteLine
        modelBuilder.Entity<CreditNoteLine>(entity =>
        {
            entity.ToTable("CreditNoteLines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.LineNet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineTax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineGross).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.CreditNoteId);
        });
    }
}
