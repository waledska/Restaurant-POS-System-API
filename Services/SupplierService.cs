using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _db;

        public SupplierService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<List<SupplierDto>>> GetAllSuppliersAsync()
        {
            var list = await _db.Suppliers
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    Address = s.Address,
                    CurrentBalance = s.CurrentBalance,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToListAsync();

            return ServiceResult<List<SupplierDto>>.Ok(list);
        }

        public async Task<ServiceResult<SupplierDto>> GetSupplierByIdAsync(Guid id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return ServiceResult<SupplierDto>.Fail("Supplier not found.");

            return ServiceResult<SupplierDto>.Ok(new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                Phone = s.Phone,
                Address = s.Address,
                CurrentBalance = s.CurrentBalance,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            });
        }

        public async Task<ServiceResult<SupplierDto>> CreateSupplierAsync(SupplierCreateDto dto)
        {
            var exists = await _db.Suppliers.AnyAsync(s => s.SupplierName == dto.SupplierName);
            if (exists) return ServiceResult<SupplierDto>.Fail("Supplier name already exists.");

            var s = new Supplier
            {
                SupplierId = Guid.NewGuid(),
                SupplierName = dto.SupplierName,
                Phone = dto.Phone,
                Address = dto.Address,
                CurrentBalance = dto.OpeningBalance,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Suppliers.Add(s);
            await _db.SaveChangesAsync();

            return await GetSupplierByIdAsync(s.SupplierId);
        }

        public async Task<ServiceResult<SupplierDto>> UpdateSupplierAsync(Guid id, SupplierUpdateDto dto)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return ServiceResult<SupplierDto>.Fail("Supplier not found.");

            if (s.SupplierName != dto.SupplierName && await _db.Suppliers.AnyAsync(x => x.SupplierName == dto.SupplierName))
                return ServiceResult<SupplierDto>.Fail("Supplier name already exists.");

            s.SupplierName = dto.SupplierName;
            s.Phone = dto.Phone;
            s.Address = dto.Address;
            s.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return await GetSupplierByIdAsync(s.SupplierId);
        }


    }
}
