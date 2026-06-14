namespace WebApisApp.DTOs.Common
{
    public class StockBalanceDto
    {
        public Guid LocationId { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }
        public DateTime? LastMovementAt { get; set; }
    }

    public class StockMovementDto
    {
        public Guid StockMovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public Guid LocationId { get; set; }
        public Guid ProductId { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupplierPaymentDto
    {
        public Guid SupplierPaymentId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public Guid SystemPaymentMethodId { get; set; }
        public string? PaymentMethodType { get; set; }
        public string? PaymentMethodDetails { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupplierPaymentCreateDto
    {
        public Guid SupplierId { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        public decimal Amount { get; set; }
        public Guid SystemPaymentMethodId { get; set; }
        public string? Notes { get; set; }
    }

    public class InitialStockDto
    {
        public Guid ProductId { get; set; }
        public Guid LocationId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
