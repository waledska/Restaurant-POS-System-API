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
    public class PurchaseController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;

        public PurchaseController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PurchaseInvoiceFilterDto filter)
        {
            var result = await _purchaseService.GetInvoicesAsync(filter);
            return Ok(ApiResponse<List<PurchaseInvoiceDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _purchaseService.GetInvoiceByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<PurchaseInvoiceDto>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseInvoiceCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _purchaseService.CreateInvoiceAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<PurchaseInvoiceDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _purchaseService.ApproveInvoiceAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPost("payments")]
        public async Task<IActionResult> AddPayment([FromBody] SupplierPaymentCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _purchaseService.AddPaymentAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SupplierPaymentDto>.Ok(result.Data!, result.Message));
        }
    }
}
