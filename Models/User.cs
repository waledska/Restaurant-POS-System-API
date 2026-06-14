namespace WebApisApp.Models
{
    public class User : BaseEntity
    {
        public static readonly string[] ValidRoles = { "Admin", "BranchManager", "WarehouseManager", "Cashier", "WarehouseWorker" };

        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string UserType { get; set; } = string.Empty; // Admin / BranchManager / WarehouseManager / Cashier / WarehouseWorker
        public Guid? LocationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public Location? Location { get; set; }
    }
}
