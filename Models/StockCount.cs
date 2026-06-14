namespace WebApisApp.Models
{
    public class StockCount : BaseEntity
    {
        public Guid StockCountId { get; set; }
        public Guid LocationId { get; set; }
        public DateTime CountDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Status { get; set; } = string.Empty; // Draft / Completed / Posted
        public DateTime? PostedAt { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Location Location { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public ICollection<StockCountItem> Items { get; set; } = new List<StockCountItem>();
    }
}
