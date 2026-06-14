using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApisApp.Helpers;
using WebApisApp.Services;

namespace WebApisApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceDto dto)
        {
            var result = await _deviceService.RegisterDeviceAsync(dto.DeviceCode, dto.DeviceName, dto.LocationId);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));

            var device = await _deviceService.GetDeviceByCodeAsync(dto.DeviceCode);
            return Ok(ApiResponse<object>.Ok(new
            {
                device!.DeviceId,
                device.DeviceCode,
                device.DeviceName,
                device.LocationId,
                device.IsActive,
                device.LastSeenAt
            }, "Device registered."));
        }

        [HttpGet("{deviceCode}")]
        public async Task<IActionResult> GetDevice(string deviceCode)
        {
            var device = await _deviceService.GetDeviceByCodeAsync(deviceCode);
            if (device == null) return NotFound(ApiResponse.Fail("Device not found."));

            return Ok(ApiResponse<object>.Ok(new
            {
                device.DeviceId,
                device.DeviceCode,
                device.DeviceName,
                device.LocationId,
                device.IsActive,
                device.LastSeenAt,
                device.LastSyncAt
            }));
        }

        [HttpPost("{deviceId}/heartbeat")]
        public async Task<IActionResult> Heartbeat(Guid deviceId)
        {
            await _deviceService.UpdateLastSeenAsync(deviceId);
            return Ok(ApiResponse.Ok("Heartbeat recorded."));
        }
    }

    public class RegisterDeviceDto
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public Guid LocationId { get; set; }
    }
}
