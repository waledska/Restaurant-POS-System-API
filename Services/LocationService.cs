using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _db;

        public LocationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<List<LocationDto>>> GetAllLocationsAsync()
        {
            var locations = await _db.Locations
                .Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    LocationCode = l.LocationCode,
                    LocationName = l.LocationName,
                    LocationType = l.LocationType,
                    Address = l.Address,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt
                }).ToListAsync();

            return ServiceResult<List<LocationDto>>.Ok(locations);
        }

        public async Task<ServiceResult<LocationDto>> GetLocationByIdAsync(Guid id)
        {
            var l = await _db.Locations.FindAsync(id);
            if (l == null) return ServiceResult<LocationDto>.Fail("Location not found.");

            return ServiceResult<LocationDto>.Ok(new LocationDto
            {
                LocationId = l.LocationId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName,
                LocationType = l.LocationType,
                Address = l.Address,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt
            });
        }

        public async Task<ServiceResult<LocationDto>> CreateLocationAsync(LocationCreateDto dto)
        {
            var exists = await _db.Locations.AnyAsync(l => l.LocationCode == dto.LocationCode);
            if (exists) return ServiceResult<LocationDto>.Fail("Location code already exists.");

            var loc = new Location
            {
                LocationId = Guid.NewGuid(),
                LocationCode = dto.LocationCode,
                LocationName = dto.LocationName,
                LocationType = dto.LocationType,
                Address = dto.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Locations.Add(loc);
            await _db.SaveChangesAsync();

            return await GetLocationByIdAsync(loc.LocationId);
        }

        public async Task<ServiceResult<LocationDto>> UpdateLocationAsync(Guid id, LocationUpdateDto dto)
        {
            var loc = await _db.Locations.FindAsync(id);
            if (loc == null) return ServiceResult<LocationDto>.Fail("Location not found.");

            if (loc.LocationCode != dto.LocationCode && await _db.Locations.AnyAsync(l => l.LocationCode == dto.LocationCode))
                return ServiceResult<LocationDto>.Fail("Location code already exists.");

            loc.LocationCode = dto.LocationCode;
            loc.LocationName = dto.LocationName;
            loc.LocationType = dto.LocationType;
            loc.Address = dto.Address;
            loc.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return await GetLocationByIdAsync(loc.LocationId);
        }
    }
}
