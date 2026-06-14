using System.Text.Json;

namespace WebApisApp.DTOs.Common
{
    public class SyncPullRequestDto
    {
        public Guid LocationId { get; set; }
        public long LastSyncVersion { get; set; }
    }

    public class SyncPullResponseDto
    {
        public long LatestServerVersion { get; set; }
        // Changed entity logs to inform the client what changed
        public List<ServerChangeLogDto> Changes { get; set; } = new List<ServerChangeLogDto>();
        // The actual serialized data rows for those entities
        public Dictionary<string, List<JsonElement>> Data { get; set; } = new Dictionary<string, List<JsonElement>>();
    }

    public class ServerChangeLogDto
    {
        public long ChangeVersion { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
    }

    public class SyncPushRequestDto
    {
        public Guid LocationId { get; set; }
        public Guid DeviceId { get; set; }
        // The offline changes to be pushed to server
        public Dictionary<string, List<JsonElement>> Data { get; set; } = new Dictionary<string, List<JsonElement>>();
    }
}
