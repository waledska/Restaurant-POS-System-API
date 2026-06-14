using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── 12) ProductionOperations ─────────────────────────────────────────────────
    public class ProductionOperationConfiguration : IEntityTypeConfiguration<ProductionOperation>
    {
        public void Configure(EntityTypeBuilder<ProductionOperation> builder)
        {
            builder.ToTable("ProductionOperations");
            builder.HasKey(x => x.ProductionOperationId);

            builder.Property(x => x.QuantityProduced).HasColumnType("decimal(18,4)");
            builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
            builder.Property(x => x.TotalCost).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.Location)
                .WithMany(l => l.ProductionOperations)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ManufacturedProduct)
                .WithMany(p => p.ProductionOperations)
                .HasForeignKey(x => x.ManufacturedProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.LocationId, x.ProductionDate })
                .HasDatabaseName("IX_ProductionOperations_LocationId_ProductionDate");

            builder.HasIndex(x => new { x.ManufacturedProductId, x.ProductionDate })
                .HasDatabaseName("IX_ProductionOperations_ManufacturedProductId_ProductionDate");
        }
    }

    // ─── 13) ProductionOperationItems ─────────────────────────────────────────────
    public class ProductionOperationItemConfiguration : IEntityTypeConfiguration<ProductionOperationItem>
    {
        public void Configure(EntityTypeBuilder<ProductionOperationItem> builder)
        {
            builder.ToTable("ProductionOperationItems");
            builder.HasKey(x => x.ProductionOperationItemId);

            builder.Property(x => x.QuantityConsumed).HasColumnType("decimal(18,4)");
            builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
            builder.Property(x => x.TotalCost).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.ProductionOperation)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.ProductionOperationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.RawProduct)
                .WithMany(p => p.ProductionOperationItems)
                .HasForeignKey(x => x.RawProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ProductionOperationId)
                .HasDatabaseName("IX_ProductionOperationItems_ProductionOperationId");

            builder.HasIndex(x => x.RawProductId)
                .HasDatabaseName("IX_ProductionOperationItems_RawProductId");

            builder.HasIndex(x => new { x.ProductionOperationId, x.RawProductId })
                .IsUnique().HasDatabaseName("UX_ProductionOperationItems_ProductionOperationId_RawProductId");
        }
    }

    // ─── 14) TransferRequests ─────────────────────────────────────────────────────
    public class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
    {
        public void Configure(EntityTypeBuilder<TransferRequest> builder)
        {
            builder.ToTable("TransferRequests");
            builder.HasKey(x => x.TransferRequestId);

            builder.Property(x => x.TransferCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.RequestMode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

            builder.HasOne(x => x.FromLocation)
                .WithMany(l => l.FromTransferRequests)
                .HasForeignKey(x => x.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ToLocation)
                .WithMany(l => l.ToTransferRequests)
                .HasForeignKey(x => x.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RequestedByUser)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Critical for offline reconciliation
            builder.HasIndex(x => x.TransferCode)
                .IsUnique().HasDatabaseName("UQ_TransferRequests_TransferCode");

            builder.HasIndex(x => new { x.FromLocationId, x.Status, x.RequestDate })
                .HasDatabaseName("IX_TransferRequests_FromLocationId_Status_RequestDate");

            builder.HasIndex(x => new { x.ToLocationId, x.Status, x.RequestDate })
                .HasDatabaseName("IX_TransferRequests_ToLocationId_Status_RequestDate");

            builder.HasIndex(x => new { x.Status, x.RequestDate })
                .HasDatabaseName("IX_TransferRequests_Status_RequestDate");
        }
    }

    // ─── 15) TransferRequestItems ─────────────────────────────────────────────────
    public class TransferRequestItemConfiguration : IEntityTypeConfiguration<TransferRequestItem>
    {
        public void Configure(EntityTypeBuilder<TransferRequestItem> builder)
        {
            builder.ToTable("TransferRequestItems");
            builder.HasKey(x => x.TransferRequestItemId);

            builder.Property(x => x.RequestedQty).HasColumnType("decimal(18,4)");
            builder.Property(x => x.ApprovedQty).HasColumnType("decimal(18,4)");
            builder.Property(x => x.ShippedQty).HasColumnType("decimal(18,4)");
            builder.Property(x => x.ReceivedQty).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.TransferRequest)
                .WithMany(t => t.Items)
                .HasForeignKey(x => x.TransferRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.TransferRequestItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TransferRequestId)
                .HasDatabaseName("IX_TransferRequestItems_TransferRequestId");

            builder.HasIndex(x => new { x.TransferRequestId, x.ProductId })
                .IsUnique().HasDatabaseName("UQ_TransferRequestItems_TransferRequestId_ProductId");
        }
    }

    // ─── 16) StockCounts ──────────────────────────────────────────────────────────
    public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
    {
        public void Configure(EntityTypeBuilder<StockCount> builder)
        {
            builder.ToTable("StockCounts");
            builder.HasKey(x => x.StockCountId);

            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

            builder.HasOne(x => x.Location)
                .WithMany(l => l.StockCounts)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.LocationId, x.CountDate })
                .HasDatabaseName("IX_StockCounts_LocationId_CountDate");

            builder.HasIndex(x => new { x.Status, x.CountDate })
                .HasDatabaseName("IX_StockCounts_Status_CountDate");
        }
    }

    // ─── 17) StockCountItems ──────────────────────────────────────────────────────
    public class StockCountItemConfiguration : IEntityTypeConfiguration<StockCountItem>
    {
        public void Configure(EntityTypeBuilder<StockCountItem> builder)
        {
            builder.ToTable("StockCountItems");
            builder.HasKey(x => x.StockCountItemId);

            builder.Property(x => x.SystemQty).HasColumnType("decimal(18,4)");
            builder.Property(x => x.ActualQty).HasColumnType("decimal(18,4)");
            builder.Property(x => x.DifferenceQty).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.StockCount)
                .WithMany(s => s.Items)
                .HasForeignKey(x => x.StockCountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.StockCountItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.StockCountId)
                .HasDatabaseName("IX_StockCountItems_StockCountId");

            builder.HasIndex(x => new { x.StockCountId, x.ProductId })
                .IsUnique().HasDatabaseName("UQ_StockCountItems_StockCountId_ProductId");
        }
    }
}
