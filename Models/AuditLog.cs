namespace WebApisApp.Models
{
    public class AuditLog
    {
        public Guid AuditLogId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public Guid UserId { get; set; }
        public Guid? DeviceId { get; set; }
        public string? Notes { get; set; }
        public Guid? LocationId { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Location? Location { get; set; }
    }
}
