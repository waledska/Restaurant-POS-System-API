using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(Guid userId, string actionType, string tableName, string? recordId = null, string? notes = null, Guid? deviceId = null);
        Task<ServiceResult<List<AuditLog>>> GetLogsAsync(string? tableName = null, Guid? userId = null, int page = 1, int pageSize = 50);
    }
}
