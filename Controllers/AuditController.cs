using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApisApp.Helpers;
using WebApisApp.Models;
using WebApisApp.Services;

namespace WebApisApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? tableName = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _auditService.GetLogsAsync(tableName, userId, page, pageSize);
            return Ok(ApiResponse<List<AuditLog>>.Ok(result.Data!));
        }
    }
}
