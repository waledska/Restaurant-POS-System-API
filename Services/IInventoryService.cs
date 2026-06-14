using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IInventoryService
    {
        Task<ServiceResult> ApplyStockMovementAsync(
            Guid locationId, Guid productId, decimal quantityChange,
            decimal unitCost, string referenceType, Guid referenceId, Guid createdByUserId);

        Task<ServiceResult> ApplyStockMovementBatchAsync(
            List<StockMovementRequest> movements, Guid createdByUserId);

        Task<ServiceResult<List<StockBalanceDto>>> GetStockBalancesAsync(Guid? locationId, int page = 1, int pageSize = 50);
        Task<ServiceResult<List<StockMovementDto>>> GetStockMovementsAsync(Guid? locationId, Guid? productId = null, int page = 1, int pageSize = 50);
        Task<ServiceResult> SetInitialStockAsync(InitialStockDto dto, Guid createdByUserId);
    }

    public class StockMovementRequest
    {
        public Guid LocationId { get; set; }
        public Guid ProductId { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal UnitCost { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public Guid ReferenceId { get; set; }
    }
}
