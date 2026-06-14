namespace WebApisApp.Models
{
    public class StockBalance : BaseEntity
    {
        public Guid StockBalanceId { get; set; }
        public Guid LocationId { get; set; }
        public Guid ProductId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }
        public DateTime? LastMovementAt { get; set; }

        // Navigation
        public Location Location { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
