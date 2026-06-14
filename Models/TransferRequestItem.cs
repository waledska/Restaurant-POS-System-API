namespace WebApisApp.Models
{
    public class TransferRequestItem : BaseEntity
    {
        public Guid TransferRequestItemId { get; set; }
        public Guid TransferRequestId { get; set; }
        public Guid ProductId { get; set; }
        public decimal RequestedQty { get; set; }
        public decimal? ApprovedQty { get; set; }
        public decimal? ShippedQty { get; set; }
        public decimal? ReceivedQty { get; set; }

        // Navigation
        public TransferRequest TransferRequest { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
