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
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("balances/{locationId?}")]
        public async Task<IActionResult> GetBalances(
            Guid? locationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (locationId == null && !User.IsInRole("Admin"))
                return Forbidden(ApiResponse.Fail("Only administrators can view balances for all locations."));

            var result = await _inventoryService.GetStockBalancesAsync(locationId, page, pageSize);
            return Ok(ApiResponse<List<StockBalanceDto>>.Ok(result.Data!));
        }

        [HttpGet("movements/{locationId?}")]
        public async Task<IActionResult> GetMovements(
            Guid? locationId,
            [FromQuery] Guid? productId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (locationId == null && !User.IsInRole("Admin"))
                return Forbidden(ApiResponse.Fail("Only administrators can view movements for all locations."));

            var result = await _inventoryService.GetStockMovementsAsync(locationId, productId, page, pageSize);
            return Ok(ApiResponse<List<StockMovementDto>>.Ok(result.Data!));
        }

        [HttpPost("initial")]
        public async Task<IActionResult> SetInitialStock([FromBody] InitialStockDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _inventoryService.SetInitialStockAsync(dto, Guid.Parse(userIdStr!));
            
            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse.Ok("Initial stock set successfully."));
        }

        private IActionResult Forbidden(ApiResponse response)
        {
            return StatusCode(403, response);
        }
    }
}
