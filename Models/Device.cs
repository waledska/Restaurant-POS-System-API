namespace WebApisApp.Models
{
    public class Device
    {
        public Guid DeviceId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public Guid LocationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Location Location { get; set; } = null!;
        public ICollection<ServerChangeLog> ChangeLogs { get; set; } = new List<ServerChangeLog>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
