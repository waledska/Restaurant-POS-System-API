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
    public class SystemPaymentMethodController : ControllerBase
    {
        private readonly ISystemPaymentMethodService _service;

        public SystemPaymentMethodController(ISystemPaymentMethodService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetPaymentMethodsAsync();
            return Ok(ApiResponse<List<SystemPaymentMethodDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetPaymentMethodByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SystemPaymentMethodDto>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SystemPaymentMethodCreateDto dto)
        {
            var result = await _service.AddPaymentMethodAsync(dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SystemPaymentMethodDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SystemPaymentMethodUpdateDto dto)
        {
            var result = await _service.UpdatePaymentMethodAsync(id, dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<SystemPaymentMethodDto>.Ok(result.Data!, "Updated successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(Guid id)
        {
            var result = await _service.TogglePaymentMethodAsync(id);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeletePaymentMethodAsync(id);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
