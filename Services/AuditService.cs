using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;

        public AuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task LogActionAsync(Guid userId, string actionType, string tableName, string? recordId = null, string? notes = null, Guid? deviceId = null)
        {
            var log = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                TableName = tableName,
                RecordId = recordId,
                ActionType = actionType,
                ActionDate = DateTime.UtcNow,
                UserId = userId,
                DeviceId = deviceId,
                Notes = notes
            };

            _db.AuditLogs.Add(log);
            return Task.CompletedTask;
            // The caller handles _db.SaveChangesAsync() to keep the audit log part of the business transaction.
        }

        public async Task<ServiceResult<List<AuditLog>>> GetLogsAsync(string? tableName = null, Guid? userId = null, int page = 1, int pageSize = 50)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(a => a.TableName == tableName);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            var logs = await query
                .OrderByDescending(a => a.ActionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<AuditLog>>.Ok(logs);
        }
    }
}
