namespace WebApisApp.Models
{
    public class StockMovement : BaseEntity
    {
        public Guid StockMovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public Guid LocationId { get; set; }
        public Guid ProductId { get; set; }
        public string MovementType { get; set; } = string.Empty;
        // PurchaseIn / TransferOut / TransferIn / ProductionConsume / ProductionIn / StockCountAdjustment / Waste
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Location Location { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
