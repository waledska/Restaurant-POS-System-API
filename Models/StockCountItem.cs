namespace WebApisApp.Models
{
    public class StockCountItem : BaseEntity
    {
        public Guid StockCountItemId { get; set; }
        public Guid StockCountId { get; set; }
        public Guid ProductId { get; set; }
        public decimal SystemQty { get; set; }
        public decimal? ActualQty { get; set; }
        public decimal? DifferenceQty { get; set; }

        // Navigation
        public StockCount StockCount { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
