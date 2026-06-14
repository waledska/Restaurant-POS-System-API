using Microsoft.EntityFrameworkCore;
using WebApisApp.Models;
using System.Reflection;

namespace WebApisApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Shared Business Tables
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SystemPaymentMethod> SystemPaymentMethods { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductRecipe> ProductRecipes { get; set; } = null!;
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; } = null!;
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = null!;
        public DbSet<SupplierPayment> SupplierPayments { get; set; } = null!;
        public DbSet<StockBalance> StockBalances { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;
        public DbSet<ProductionOperation> ProductionOperations { get; set; } = null!;
        public DbSet<ProductionOperationItem> ProductionOperationItems { get; set; } = null!;
        public DbSet<TransferRequest> TransferRequests { get; set; } = null!;
        public DbSet<TransferRequestItem> TransferRequestItems { get; set; } = null!;
        public DbSet<StockCount> StockCounts { get; set; } = null!;
        public DbSet<StockCountItem> StockCountItems { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // Server-Only Tables
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<ServerChangeLog> ServerChangeLog { get; set; } = null!;
        public DbSet<GlobalSettings> GlobalSettings { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Apply all configurations from the current assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
