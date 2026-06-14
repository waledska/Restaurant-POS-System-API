using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IStockCountService
    {
        Task<ServiceResult<List<StockCountDto>>> GetCountsAsync(Guid locationId, int page = 1, int pageSize = 50);
        Task<ServiceResult<StockCountDto>> CreateStockCountAsync(StockCountCreateDto dto, Guid createdByUserId);
        Task<ServiceResult<StockCountDetailDto>> GetCountByIdAsync(Guid stockCountId);
        Task<ServiceResult> PostStockCountAsync(Guid stockCountId, Guid userId);
        Task<ServiceResult> DeleteStockCountAsync(Guid stockCountId, Guid userId);
    }
}
