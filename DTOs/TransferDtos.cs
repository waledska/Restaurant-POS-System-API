namespace WebApisApp.DTOs.Common
{
    public class TransferRequestDto
    {
        public Guid TransferRequestId { get; set; }
        public string TransferCode { get; set; } = string.Empty;
        public Guid FromLocationId { get; set; }
        public Guid ToLocationId { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Accepted, Preparing, Ready, InTransit, Received, Rejected
        public string? RejectionReason { get; set; }
        public DateTime? PreparedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public List<TransferRequestItemDto> Items { get; set; } = new List<TransferRequestItemDto>();
    }

    public class TransferRequestItemDto
    {
        public Guid ProductId { get; set; }
        public decimal RequestedQuantity { get; set; }
    }

    public class TransferRequestCreateDto
    {
        public Guid FromLocationId { get; set; }
        public Guid ToLocationId { get; set; }
        public List<TransferRequestItemDto> Items { get; set; } = new List<TransferRequestItemDto>();
    }

    public class RejectTransferDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
