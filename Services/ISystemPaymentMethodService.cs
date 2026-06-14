using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface ISystemPaymentMethodService
    {
        Task<ServiceResult<List<SystemPaymentMethodDto>>> GetPaymentMethodsAsync();
        Task<ServiceResult<SystemPaymentMethodDto>> GetPaymentMethodByIdAsync(Guid id);
        Task<ServiceResult<SystemPaymentMethodDto>> AddPaymentMethodAsync(SystemPaymentMethodCreateDto dto);
        Task<ServiceResult> TogglePaymentMethodAsync(Guid methodId);
        Task<ServiceResult<SystemPaymentMethodDto>> UpdatePaymentMethodAsync(Guid methodId, SystemPaymentMethodUpdateDto dto);
        Task<ServiceResult> DeletePaymentMethodAsync(Guid methodId);
    }
}
