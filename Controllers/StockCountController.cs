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
    public class StockCountController : ControllerBase
    {
        private readonly IStockCountService _stockCountService;

        public StockCountController(IStockCountService stockCountService)
        {
            _stockCountService = stockCountService;
        }

        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetCounts(
            Guid locationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _stockCountService.GetCountsAsync(locationId, page, pageSize);
            return Ok(ApiResponse<List<StockCountDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _stockCountService.GetCountByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<StockCountDetailDto>.Ok(result.Data!));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockCountCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _stockCountService.CreateStockCountAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<StockCountDto>.Ok(result.Data!, "Created successfully."));
        }

        [HttpPost("{id}/post")]
        public async Task<IActionResult> PostCount(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _stockCountService.PostStockCountAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _stockCountService.DeleteStockCountAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
