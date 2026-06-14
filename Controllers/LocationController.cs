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
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _locationService.GetAllLocationsAsync();
            return Ok(ApiResponse<List<LocationDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _locationService.GetLocationByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<LocationDto>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LocationCreateDto dto)
        {
            var result = await _locationService.CreateLocationAsync(dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<LocationDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] LocationUpdateDto dto)
        {
            var result = await _locationService.UpdateLocationAsync(id, dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<LocationDto>.Ok(result.Data!, "Updated successfully."));
        }
    }
}
