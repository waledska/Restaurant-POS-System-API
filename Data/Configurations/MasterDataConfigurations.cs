using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApisApp.Models;

namespace WebApisApp.Data.Configurations
{
    // ─── 1) Locations ───────────────────────────────────────────────────────────
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.ToTable("Locations");
            builder.HasKey(x => x.LocationId);

            builder.Property(x => x.LocationCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.LocationName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.LocationType).HasMaxLength(50).IsRequired();

            builder.HasIndex(x => x.LocationCode)
                .IsUnique().HasDatabaseName("UX_Locations_LocationCode");

            builder.HasIndex(x => new { x.LocationType, x.IsActive })
                .HasDatabaseName("IX_Locations_LocationType_IsActive");

            builder.HasIndex(x => x.LocationName)
                .HasDatabaseName("IX_Locations_LocationName");
        }
    }

    // ─── 2) Users ────────────────────────────────────────────────────────────────
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.Property(x => x.UserType).HasMaxLength(50).IsRequired();

            builder.HasOne(x => x.Location)
                .WithMany(l => l.Users)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserName)
                .IsUnique().HasDatabaseName("UX_Users_UserName");

            builder.HasIndex(x => x.LocationId)
                .HasDatabaseName("IX_Users_LocationId");

            builder.HasIndex(x => new { x.UserType, x.IsActive })
                .HasDatabaseName("IX_Users_UserType_IsActive");

            builder.HasIndex(x => x.Email)
                .HasDatabaseName("IX_Users_Email");
        }
    }

    // ─── 3) Suppliers ────────────────────────────────────────────────────────────
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");
            builder.HasKey(x => x.SupplierId);

            builder.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.CurrentBalance).HasColumnType("decimal(18,4)");

            builder.HasIndex(x => x.SupplierName)
                .HasDatabaseName("IX_Suppliers_SupplierName");

            builder.HasIndex(x => x.Phone)
                .HasDatabaseName("IX_Suppliers_Phone");

            builder.HasIndex(x => new { x.IsActive, x.SupplierName })
                .HasDatabaseName("IX_Suppliers_IsActive_SupplierName");
        }
    }

    // ─── 4) SystemPaymentMethods ───────────────────────────────────────────────
    public class SystemPaymentMethodConfiguration : IEntityTypeConfiguration<SystemPaymentMethod>
    {
        public void Configure(EntityTypeBuilder<SystemPaymentMethod> builder)
        {
            builder.ToTable("SystemPaymentMethods");
            builder.HasKey(x => x.SystemPaymentMethodId);

            builder.Property(x => x.MethodType).HasMaxLength(50).IsRequired();

            builder.HasIndex(x => new { x.MethodType, x.IsActive })
                .HasDatabaseName("IX_SystemPaymentMethods_MethodType_IsActive");
        }
    }

    // ─── 5) Products ─────────────────────────────────────────────────────────────
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(x => x.ProductId);

            builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ProductType).HasMaxLength(50).IsRequired();
            builder.Property(x => x.BaseUnitName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.GlobalAverageCost).HasColumnType("decimal(18,4)");
            builder.Property(x => x.SellingPrice).HasColumnType("decimal(18,4)");

            builder.HasIndex(x => x.ProductCode)
                .IsUnique().HasDatabaseName("UX_Products_ProductCode");

            builder.HasIndex(x => x.ProductName)
                .HasDatabaseName("IX_Products_ProductName");

            builder.HasIndex(x => new { x.ProductType, x.IsActive })
                .HasDatabaseName("IX_Products_ProductType_IsActive");
        }
    }

    // ─── 6) ProductRecipes ───────────────────────────────────────────────────────
    public class ProductRecipeConfiguration : IEntityTypeConfiguration<ProductRecipe>
    {
        public void Configure(EntityTypeBuilder<ProductRecipe> builder)
        {
            builder.ToTable("ProductRecipes");
            builder.HasKey(x => x.RecipeId);

            builder.Property(x => x.QuantityNeeded).HasColumnType("decimal(18,4)");

            builder.HasOne(x => x.ManufacturedProduct)
                .WithMany(p => p.RecipesAsManufactured)
                .HasForeignKey(x => x.ManufacturedProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RawProduct)
                .WithMany(p => p.RecipesAsRaw)
                .HasForeignKey(x => x.RawProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ManufacturedProductId, x.RawProductId })
                .IsUnique().HasDatabaseName("UQ_ProductRecipes_ManufacturedProductId_RawProductId");

            builder.HasIndex(x => x.RawProductId)
                .HasDatabaseName("IX_ProductRecipes_RawProductId");
        }
    }
}
