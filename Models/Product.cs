namespace WebApisApp.Models
{
    public class Product : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty; // Raw / Manufactured
        public string BaseUnitName { get; set; } = string.Empty;
        public decimal GlobalAverageCost { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public ICollection<ProductRecipe> RecipesAsManufactured { get; set; } = new List<ProductRecipe>();
        public ICollection<ProductRecipe> RecipesAsRaw { get; set; } = new List<ProductRecipe>();
        public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<ProductionOperation> ProductionOperations { get; set; } = new List<ProductionOperation>();
        public ICollection<ProductionOperationItem> ProductionOperationItems { get; set; } = new List<ProductionOperationItem>();
        public ICollection<TransferRequestItem> TransferRequestItems { get; set; } = new List<TransferRequestItem>();
        public ICollection<StockCountItem> StockCountItems { get; set; } = new List<StockCountItem>();
    }
}
