using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApisApp.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Track online connections: ConnectionId -> { UserId, LocationId }
        private static readonly ConcurrentDictionary<string, ConnectionInfo> OnlineConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var locationId = Context.User?.FindFirst("LocationId")?.Value;

            var info = new ConnectionInfo
            {
                UserId = userId ?? string.Empty,
                LocationId = locationId ?? string.Empty
            };

            OnlineConnections[Context.ConnectionId] = info;

            // Join location group
            if (!string.IsNullOrEmpty(locationId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Location_{locationId}");
            }

            // Join general presence group
            await Groups.AddToGroupAsync(Context.ConnectionId, "OnlineDevices");

            // Broadcast updated presence to all connected clients
            await BroadcastPresenceAsync();

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            OnlineConnections.TryRemove(Context.ConnectionId, out var info);

            if (info != null && !string.IsNullOrEmpty(info.LocationId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Location_{info.LocationId}");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "OnlineDevices");

            // Broadcast updated presence after disconnect
            await BroadcastPresenceAsync();

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client can call this to get the current online locations.
        /// </summary>
        public async Task RequestOnlineStatus()
        {
            await BroadcastPresenceAsync();
        }

        private async Task BroadcastPresenceAsync()
        {
            var onlineLocations = OnlineConnections.Values
                .Where(c => !string.IsNullOrEmpty(c.LocationId))
                .Select(c => c.LocationId)
                .Distinct()
                .ToList();

            var onlineUsers = OnlineConnections.Values
                .Where(c => !string.IsNullOrEmpty(c.UserId))
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            await Clients.Group("OnlineDevices").SendAsync("OnlinePresenceUpdated", new
            {
                OnlineLocationIds = onlineLocations,
                OnlineUserIds = onlineUsers,
                Timestamp = DateTime.UtcNow
            });
        }

        private class ConnectionInfo
        {
            public string UserId { get; set; } = string.Empty;
            public string LocationId { get; set; } = string.Empty;
        }
    }
}
