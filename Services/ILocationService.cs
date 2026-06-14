using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface ILocationService
    {
        Task<ServiceResult<List<LocationDto>>> GetAllLocationsAsync();
        Task<ServiceResult<LocationDto>> GetLocationByIdAsync(Guid id);
        Task<ServiceResult<LocationDto>> CreateLocationAsync(LocationCreateDto dto);
        Task<ServiceResult<LocationDto>> UpdateLocationAsync(Guid id, LocationUpdateDto dto);
    }
}
