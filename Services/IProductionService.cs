using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IProductionService
    {
        Task<ServiceResult<List<ProductionOperationDto>>> GetProductionsAsync(Guid locationId, int page = 1, int pageSize = 50);
        Task<ServiceResult<ProductionOperationDto>> CreateProductionAsync(ProductionCreateDto dto, Guid createdByUserId);
        Task<ServiceResult> DeleteProductionAsync(Guid productionId, Guid userId);
    }
}
