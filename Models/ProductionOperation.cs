namespace WebApisApp.Models
{
    public class ProductionOperation : BaseEntity
    {
        public Guid ProductionOperationId { get; set; }
        public Guid LocationId { get; set; }
        public Guid ManufacturedProductId { get; set; }
        public decimal QuantityProduced { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime ProductionDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Location Location { get; set; } = null!;
        public Product ManufacturedProduct { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public ICollection<ProductionOperationItem> Items { get; set; } = new List<ProductionOperationItem>();
    }
}
