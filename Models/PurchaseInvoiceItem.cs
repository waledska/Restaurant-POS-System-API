namespace WebApisApp.Models
{
    public class PurchaseInvoiceItem : BaseEntity
    {
        public Guid PurchaseInvoiceItemId { get; set; }
        public Guid PurchaseInvoiceId { get; set; }
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        // Navigation
        public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
