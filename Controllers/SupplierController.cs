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
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        private readonly IPurchaseService _purchaseService;

        public SupplierController(ISupplierService supplierService, IPurchaseService purchaseService)
        {
            _supplierService = supplierService;
            _purchaseService = purchaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _supplierService.GetAllSuppliersAsync();
            return Ok(ApiResponse<List<SupplierDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SupplierDto>.Ok(result.Data!));
        }

        [HttpGet("{id}/payments")]
        public async Task<IActionResult> GetPayments(Guid id)
        {
            var result = await _purchaseService.GetSupplierPaymentsAsync(id);
            return Ok(ApiResponse<List<SupplierPaymentDto>>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupplierCreateDto dto)
        {
            var result = await _supplierService.CreateSupplierAsync(dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SupplierDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SupplierUpdateDto dto)
        {
            var result = await _supplierService.UpdateSupplierAsync(id, dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SupplierDto>.Ok(result.Data!, "Updated successfully."));
        }


    }
}
