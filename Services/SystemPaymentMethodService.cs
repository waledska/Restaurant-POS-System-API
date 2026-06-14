using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class SystemPaymentMethodService : ISystemPaymentMethodService
    {
        private readonly ApplicationDbContext _db;

        public SystemPaymentMethodService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<List<SystemPaymentMethodDto>>> GetPaymentMethodsAsync()
        {
            var methods = await _db.SystemPaymentMethods
                .Where(m => !m.IsDeleted)
                .Select(m => new SystemPaymentMethodDto
                {
                    SystemPaymentMethodId = m.SystemPaymentMethodId,
                    MethodType = m.MethodType,
                    Details = m.AccountData,
                    Notes = m.Notes,
                    IsActive = m.IsActive
                }).ToListAsync();

            return ServiceResult<List<SystemPaymentMethodDto>>.Ok(methods);
        }

        public async Task<ServiceResult<SystemPaymentMethodDto>> GetPaymentMethodByIdAsync(Guid id)
        {
            var m = await _db.SystemPaymentMethods.FirstOrDefaultAsync(x => x.SystemPaymentMethodId == id && !x.IsDeleted);
            if (m == null) return ServiceResult<SystemPaymentMethodDto>.Fail("Payment method not found.");

            return ServiceResult<SystemPaymentMethodDto>.Ok(new SystemPaymentMethodDto
            {
                SystemPaymentMethodId = m.SystemPaymentMethodId,
                MethodType = m.MethodType,
                Details = m.AccountData,
                Notes = m.Notes,
                IsActive = m.IsActive
            });
        }

        public async Task<ServiceResult<SystemPaymentMethodDto>> AddPaymentMethodAsync(SystemPaymentMethodCreateDto dto)
        {
            var method = new SystemPaymentMethod
            {
                SystemPaymentMethodId = Guid.NewGuid(),
                MethodType = dto.MethodType,
                AccountData = dto.Details,
                Notes = dto.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.SystemPaymentMethods.Add(method);
            await _db.SaveChangesAsync();

            return ServiceResult<SystemPaymentMethodDto>.Ok(new SystemPaymentMethodDto
            {
                SystemPaymentMethodId = method.SystemPaymentMethodId,
                MethodType = method.MethodType,
                Details = method.AccountData,
                Notes = method.Notes,
                IsActive = method.IsActive
            });
        }

        public async Task<ServiceResult> TogglePaymentMethodAsync(Guid methodId)
        {
            var m = await _db.SystemPaymentMethods.FindAsync(methodId);
            if (m == null) return ServiceResult.Fail("Payment method not found.");

            m.IsActive = !m.IsActive;
            await _db.SaveChangesAsync();

            return ServiceResult.Ok($"Payment method active status: {m.IsActive}");
        }

        public async Task<ServiceResult<SystemPaymentMethodDto>> UpdatePaymentMethodAsync(Guid methodId, SystemPaymentMethodUpdateDto dto)
        {
            var m = await _db.SystemPaymentMethods.FindAsync(methodId);
            if (m == null) return ServiceResult<SystemPaymentMethodDto>.Fail("Payment method not found.");

            m.MethodType = dto.MethodType;
            m.AccountData = dto.Details;
            m.Notes = dto.Notes;
            m.IsActive = dto.IsActive;
            m.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<SystemPaymentMethodDto>.Ok(new SystemPaymentMethodDto
            {
                SystemPaymentMethodId = m.SystemPaymentMethodId,
                MethodType = m.MethodType,
                Details = m.AccountData,
                Notes = m.Notes,
                IsActive = m.IsActive
            });
        }

        public async Task<ServiceResult> DeletePaymentMethodAsync(Guid methodId)
        {
            var m = await _db.SystemPaymentMethods.FindAsync(methodId);
            if (m == null) return ServiceResult.Fail("Payment method not found.");

            // Soft delete
            m.IsDeleted = true;
            m.UpdatedAt = DateTime.UtcNow;
            
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Payment method deleted successfully.");
        }
    }
}
