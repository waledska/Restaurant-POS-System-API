namespace WebApisApp.Models
{
    public class TransferRequest : BaseEntity
    {
        public Guid TransferRequestId { get; set; }
        public string TransferCode { get; set; } = string.Empty;
        public Guid FromLocationId { get; set; }
        public Guid ToLocationId { get; set; }
        public DateTime RequestDate { get; set; }
        public Guid RequestedByUserId { get; set; }
        public string RequestMode { get; set; } = string.Empty; // Online / Offline
        public string Status { get; set; } = string.Empty;
        // Pending / Accepted / Rejected / Preparing / Ready / InTransit / Received / ReceiptRejected
        public DateTime? PreparedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? RejectedReason { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Location FromLocation { get; set; } = null!;
        public Location ToLocation { get; set; } = null!;
        public User RequestedByUser { get; set; } = null!;
        public ICollection<TransferRequestItem> Items { get; set; } = new List<TransferRequestItem>();
    }
}
