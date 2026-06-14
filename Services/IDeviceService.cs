using WebApisApp.Models;

namespace WebApisApp.Services
{
    public interface IDeviceService
    {
        Task<Device?> GetDeviceByCodeAsync(string deviceCode);
        Task UpdateLastSeenAsync(Guid deviceId);
        Task<WebApisApp.Helpers.ServiceResult> RegisterDeviceAsync(string deviceCode, string deviceName, Guid locationId);
    }
}
