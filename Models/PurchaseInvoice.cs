namespace WebApisApp.Models
{
    public class PurchaseInvoice : BaseEntity
    {
        public Guid PurchaseInvoiceId { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid LocationId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Open / PartiallyPaid / Paid / Cancelled
        public Guid CreatedByUserId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Supplier Supplier { get; set; } = null!;
        public Location Location { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
        public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
    }
}
