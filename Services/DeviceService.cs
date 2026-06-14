using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly ApplicationDbContext _db;

        public DeviceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Device?> GetDeviceByCodeAsync(string deviceCode)
        {
            return await _db.Devices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
        }

        public async Task UpdateLastSeenAsync(Guid deviceId)
        {
            var device = await _db.Devices.FindAsync(deviceId);
            if (device != null)
            {
                device.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<ServiceResult> RegisterDeviceAsync(string deviceCode, string deviceName, Guid locationId)
        {
            var targetLocation = await _db.Locations.FindAsync(locationId);
            if (targetLocation == null)
                return ServiceResult.Fail("Specified LocationId does not exist.");

            var existing = await _db.Devices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
            if (existing != null)
            {
                existing.DeviceName = deviceName;
                existing.LocationId = locationId;
                existing.LastSeenAt = DateTime.UtcNow;
            }
            else
            {
                var device = new Device
                {
                    DeviceId = Guid.NewGuid(),
                    DeviceCode = deviceCode,
                    DeviceName = deviceName,
                    LocationId = locationId,
                    IsActive = true,
                    LastSeenAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Devices.Add(device);
            }
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Device registered successfully.");
        }
    }
}
