using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface ISupplierService
    {
        Task<ServiceResult<List<SupplierDto>>> GetAllSuppliersAsync();
        Task<ServiceResult<SupplierDto>> GetSupplierByIdAsync(Guid id);
        Task<ServiceResult<SupplierDto>> CreateSupplierAsync(SupplierCreateDto dto);
        Task<ServiceResult<SupplierDto>> UpdateSupplierAsync(Guid id, SupplierUpdateDto dto);
        

    }
}
