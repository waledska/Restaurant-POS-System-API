namespace WebApisApp.Models
{
    public class SupplierPayment : BaseEntity
    {
        public Guid SupplierPaymentId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public Guid SystemPaymentMethodId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Supplier Supplier { get; set; } = null!;
        public PurchaseInvoice? PurchaseInvoice { get; set; }
        public SystemPaymentMethod SystemPaymentMethod { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
