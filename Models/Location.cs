namespace WebApisApp.Models
{
    public class Location : BaseEntity
    {
        public Guid LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty; // Branch / Warehouse
        public string? Address { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
        public ICollection<ProductionOperation> ProductionOperations { get; set; } = new List<ProductionOperation>();
        public ICollection<TransferRequest> FromTransferRequests { get; set; } = new List<TransferRequest>();
        public ICollection<TransferRequest> ToTransferRequests { get; set; } = new List<TransferRequest>();
        public ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
    }
}
