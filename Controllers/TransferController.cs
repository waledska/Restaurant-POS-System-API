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
    public class TransferController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransferController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetTransfers(
            Guid locationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _transferService.GetTransfersAsync(locationId, page, pageSize);
            return Ok(ApiResponse<List<TransferRequestDto>>.Ok(result.Data!));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferRequestCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.CreateTransferRequestAsync(dto, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<TransferRequestDto>.Ok(result.Data!, "Transfer created successfully."));
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.AcceptTransferAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTransferDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.RejectTransferAsync(id, dto.Reason, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("{id}/prepare")]
        public async Task<IActionResult> Prepare(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.PrepareTransferAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("{id}/ship")]
        public async Task<IActionResult> Ship(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.ShipTransferAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> Receive(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.ReceiveTransferAsync(id, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("{id}/reject-receipt")]
        public async Task<IActionResult> RejectReceipt(Guid id, [FromBody] RejectTransferDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _transferService.RejectReceiptAsync(id, dto.Reason, Guid.Parse(userIdStr!));
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
