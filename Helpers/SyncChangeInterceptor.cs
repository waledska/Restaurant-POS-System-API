using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using WebApisApp.Models;

namespace WebApisApp.Helpers
{
    public class SyncChangeInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly object _versionLock = new object();

        public SyncChangeInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ProcessSyncChanges(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ProcessSyncChanges(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ProcessSyncChanges(DbContext? context)
        {
            if (context == null) return;

            // Filter ONLY ISyncEntity entries (excludes ServerChangeLog, RefreshToken, BlacklistedToken, etc.)
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is ISyncEntity && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any()) return;

            // Extract user context safely (will be null during startup seed operations)
            Guid? userId = null;
            Guid? locationId = null;
            Guid? deviceId = null;
            
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User != null)
                {
                    var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var locationIdStr = httpContext.User.FindFirst("LocationId")?.Value;
                    var deviceIdStr = httpContext.User.FindFirst("DeviceId")?.Value;

                    userId = Guid.TryParse(userIdStr, out var u) ? u : null;
                    locationId = Guid.TryParse(locationIdStr, out var l) ? l : null;
                    deviceId = Guid.TryParse(deviceIdStr, out var d) ? d : null;
                }
            }
            catch { /* HttpContext not available (e.g. during startup seed) */ }

            // Get current max version from already-tracked (local) ServerChangeLog entries first,
            // then fall back to a safe DB query. This avoids querying the DB during active SaveChanges.
            long currentMaxVersion = 0;
            try
            {
                // Check local tracker first for any pending ServerChangeLog entries
                var localMaxVersion = context.ChangeTracker.Entries<ServerChangeLog>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Unchanged || e.State == EntityState.Modified)
                    .Select(e => e.Entity.ChangeVersion)
                    .DefaultIfEmpty(0)
                    .Max();

                if (localMaxVersion > 0)
                {
                    currentMaxVersion = localMaxVersion;
                }
                else
                {
                    // Safe DB query — only runs when no local entries exist
                    currentMaxVersion = context.Set<ServerChangeLog>()
                        .OrderByDescending(x => x.ChangeVersion)
                        .Select(x => x.ChangeVersion)
                        .FirstOrDefault();
                }
            }
            catch { /* If DB query fails, start from 0 */ }

            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                var entity = (ISyncEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    // Only set CreatedAt if it hasn't been explicitly set
                    if (entity.CreatedAt == default)
                    {
                        entity.CreatedAt = now;
                    }
                }

                entity.UpdatedAt = now;

                // Handle Soft Delete: transform hard delete into a soft delete
                // UNLESS it's a mapping/relationship entity that should be cleaned up.
                if (entry.State == EntityState.Deleted)
                {
                    if (entry.Entity is ProductRecipe)
                    {
                        // Hard Delete: Let EF Core remove the row, but we still log it below.
                    }
                    else
                    {
                        entity.IsDeleted = true;
                        entity.UpdatedAt = now;
                        entry.State = EntityState.Modified;
                    }
                }

                // Determine operation type for the change log
                string operationType;
                if (entity.IsDeleted || entry.State == EntityState.Deleted)
                    operationType = "Delete";
                else if (entry.State == EntityState.Added)
                    operationType = "Insert";
                else
                    operationType = "Update";

                var entityName = entry.Entity.GetType().Name;
                var entityId = GetPrimaryKeyValue(entry);

                currentMaxVersion++;

                var log = new ServerChangeLog
                {
                    ChangeVersion = currentMaxVersion,
                    EntityName = entityName,
                    EntityId = entityId,
                    OperationType = operationType,
                    ChangedAt = now,
                    LocationId = locationId,
                    ChangedByUserId = userId,
                    DeviceId = deviceId
                };

                context.Set<ServerChangeLog>().Add(log);
            }
        }

        private string GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var keyName = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).FirstOrDefault();
            if (keyName == null) return "Unknown";

            var value = entry.Property(keyName).CurrentValue;
            return value?.ToString() ?? "Unknown";
        }
    }
}
