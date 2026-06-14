namespace WebApisApp.Models
{
    public class ServerChangeLog
    {
        public long ChangeId { get; set; }          // bigint, PK, identity
        public long ChangeVersion { get; set; }     // bigint, unique — used for incremental sync
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? ChangedByUserId { get; set; }
        public Guid? DeviceId { get; set; }

        // Navigation
        public Location? Location { get; set; }
        public User? ChangedByUser { get; set; }
        public Device? Device { get; set; }
    }
}
