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
    public class ProductionController : ControllerBase
    {
        private readonly IProductionService _productionService;

        public ProductionController(IProductionService productionService)
        {
            _productionService = productionService;
        }

        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetProductions(
            Guid locationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _productionService.GetProductionsAsync(locationId, page, pageSize);
            return Ok(ApiResponse<List<ProductionOperationDto>>.Ok(result.Data!));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductionCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _productionService.CreateProductionAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<ProductionOperationDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _productionService.DeleteProductionAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
