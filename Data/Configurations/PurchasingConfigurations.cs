using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── 7) PurchaseInvoices ─────────────────────────────────────────────────────
    public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
        {
            builder.ToTable("PurchaseInvoices");
            builder.HasKey(x => x.PurchaseInvoiceId);

            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,4)");
            builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,4)");
            builder.Property(x => x.RemainingAmount).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.Supplier)
                .WithMany(s => s.PurchaseInvoices)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Location)
                .WithMany(l => l.PurchaseInvoices)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => new { x.SupplierId, x.InvoiceDate })
                .HasDatabaseName("IX_PurchaseInvoices_SupplierId_InvoiceDate");

            builder.HasIndex(x => new { x.LocationId, x.InvoiceDate })
                .HasDatabaseName("IX_PurchaseInvoices_LocationId_InvoiceDate");

            builder.HasIndex(x => new { x.Status, x.InvoiceDate })
                .HasDatabaseName("IX_PurchaseInvoices_Status_InvoiceDate");

            builder.HasIndex(x => x.InvoiceNumber)
                .HasDatabaseName("IX_PurchaseInvoices_InvoiceNumber");
        }
    }

    // ─── 8) PurchaseInvoiceItems ─────────────────────────────────────────────────
    public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
        {
            builder.ToTable("PurchaseInvoiceItems");
            builder.HasKey(x => x.PurchaseInvoiceItemId);

            builder.Property(x => x.UnitName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            builder.Property(x => x.LineTotal).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.PurchaseInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(x => x.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.PurchaseInvoiceItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.PurchaseInvoiceId)
                .HasDatabaseName("IX_PurchaseInvoiceItems_PurchaseInvoiceId");

            builder.HasIndex(x => x.ProductId)
                .HasDatabaseName("IX_PurchaseInvoiceItems_ProductId");

            builder.HasIndex(x => new { x.PurchaseInvoiceId, x.ProductId })
                .HasDatabaseName("IX_PurchaseInvoiceItems_PurchaseInvoiceId_ProductId");
        }
    }

    // ─── 9) SupplierPayments ─────────────────────────────────────────────────────
    public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SupplierPayments");
            builder.HasKey(x => x.SupplierPaymentId);

            builder.Property(x => x.Amount).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.Supplier)
                .WithMany(s => s.Payments)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PurchaseInvoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.SystemPaymentMethod)
                .WithMany(m => m.SupplierPayments)
                .HasForeignKey(x => x.SystemPaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => new { x.SupplierId, x.PaymentDate })
                .HasDatabaseName("IX_SupplierPayments_SupplierId_PaymentDate");

            builder.HasIndex(x => x.PurchaseInvoiceId)
                .HasDatabaseName("IX_SupplierPayments_PurchaseInvoiceId");

            builder.HasIndex(x => x.SystemPaymentMethodId)
                .HasDatabaseName("IX_SupplierPayments_SystemPaymentMethodId");
        }
    }
}
