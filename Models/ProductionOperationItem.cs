namespace WebApisApp.Models
{
    public class ProductionOperationItem : BaseEntity
    {
        public Guid ProductionOperationItemId { get; set; }
        public Guid ProductionOperationId { get; set; }
        public Guid RawProductId { get; set; }
        public decimal QuantityConsumed { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }

        // Navigation
        public ProductionOperation ProductionOperation { get; set; } = null!;
        public Product RawProduct { get; set; } = null!;
    }
}
