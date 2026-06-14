namespace WebApisApp.DTOs.Common
{
    public class PurchaseInvoiceDto
    {
        public Guid PurchaseInvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid SupplierId { get; set; }
        public Guid LocationId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new List<PurchaseInvoiceItemDto>();
    }

    public class PurchaseInvoiceItemDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PurchaseInvoiceItemCreateDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseInvoiceCreateDto
    {
        public Guid SupplierId { get; set; }
        public Guid LocationId { get; set; }
        public List<PurchaseInvoiceItemCreateDto> Items { get; set; } = new List<PurchaseInvoiceItemCreateDto>();
    }

    public class PurchaseInvoiceFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public Guid? LocationId { get; set; }
        public Guid? SupplierId { get; set; }
        public string? Status { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
