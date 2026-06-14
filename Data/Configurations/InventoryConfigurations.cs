using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── 10) StockBalances ────────────────────────────────────────────────────────
    public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
    {
        public void Configure(EntityTypeBuilder<StockBalance> builder)
        {
            builder.ToTable("StockBalances");
            builder.HasKey(x => x.StockBalanceId);

            builder.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,4)");
            builder.Property(x => x.AverageCost).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.Location)
                .WithMany(l => l.StockBalances)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.StockBalances)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Critical unique composite index for fast balance lookup
            builder.HasIndex(x => new { x.LocationId, x.ProductId })
                .IsUnique().HasDatabaseName("UX_StockBalances_LocationId_ProductId");

            builder.HasIndex(x => x.ProductId)
                .HasDatabaseName("IX_StockBalances_ProductId");
        }
    }

    // ─── 11) StockMovements ───────────────────────────────────────────────────────
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("StockMovements");
            builder.HasKey(x => x.StockMovementId);

            builder.Property(x => x.MovementType).HasMaxLength(50).IsRequired();
            builder.Property(x => x.QuantityIn).HasColumnType("decimal(18,4)");
            builder.Property(x => x.QuantityOut).HasColumnType("decimal(18,4)");
            builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
            builder.Property(x => x.TotalCost).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.Location)
                .WithMany(l => l.StockMovements)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes — keep lean on this write-heavy table
            builder.HasIndex(x => new { x.LocationId, x.ProductId, x.MovementDate })
                .HasDatabaseName("IX_StockMovements_LocationId_ProductId_MovementDate");

            builder.HasIndex(x => new { x.ProductId, x.MovementDate })
                .HasDatabaseName("IX_StockMovements_ProductId_MovementDate");

            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId })
                .HasDatabaseName("IX_StockMovements_ReferenceType_ReferenceId");
        }
    }
}
