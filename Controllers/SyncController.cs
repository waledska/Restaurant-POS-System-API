using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Services;

namespace WebApisApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;

        public SyncController(ISyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("pull")]
        public async Task<IActionResult> PullChanges([FromBody] SyncPullRequestDto dto)
        {
            var result = await _syncService.PullChangesAsync(dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SyncPullResponseDto>.Ok(result.Data!));
        }

        [HttpPost("push")]
        public async Task<IActionResult> PushChanges([FromBody] SyncPushRequestDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _syncService.PushChangesAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
