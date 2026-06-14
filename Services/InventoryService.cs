using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult> ApplyStockMovementAsync(
            Guid locationId, Guid productId, decimal quantityChange, decimal unitCost,
            string referenceType, Guid referenceId, Guid createdByUserId)
        {
            return await ApplyStockMovementBatchAsync(new List<StockMovementRequest>
            {
                new StockMovementRequest
                {
                    LocationId = locationId,
                    ProductId = productId,
                    QuantityChange = quantityChange,
                    UnitCost = unitCost,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId
                }
            }, createdByUserId);
        }

        public async Task<ServiceResult> ApplyStockMovementBatchAsync(
            List<StockMovementRequest> movements, Guid createdByUserId)
        {
            if (movements == null || !movements.Any())
                return ServiceResult.Ok();

            foreach (var req in movements)
            {
                var balance = await _db.StockBalances
                    .FirstOrDefaultAsync(b => b.LocationId == req.LocationId && b.ProductId == req.ProductId);

                if (balance == null)
                {
                    balance = new StockBalance
                    {
                        StockBalanceId = Guid.NewGuid(),
                        LocationId = req.LocationId,
                        ProductId = req.ProductId,
                        QuantityOnHand = 0,
                        AverageCost = 0,
                        LastMovementAt = DateTime.UtcNow
                    };
                    _db.StockBalances.Add(balance);
                }

                // Weighted average cost calculation on inbound
                if (req.QuantityChange > 0)
                {
                    var oldTotalVal = balance.QuantityOnHand * balance.AverageCost;
                    var newTotalVal = req.QuantityChange * req.UnitCost;
                    var newQty = balance.QuantityOnHand + req.QuantityChange;
                    if (newQty > 0)
                        balance.AverageCost = (oldTotalVal + newTotalVal) / newQty;
                }

                balance.QuantityOnHand += req.QuantityChange;
                balance.LastMovementAt = DateTime.UtcNow;

                var movement = new StockMovement
                {
                    StockMovementId = Guid.NewGuid(),
                    LocationId = req.LocationId,
                    ProductId = req.ProductId,
                    QuantityIn = req.QuantityChange > 0 ? req.QuantityChange : 0,
                    QuantityOut = req.QuantityChange < 0 ? Math.Abs(req.QuantityChange) : 0,
                    UnitCost = req.UnitCost,
                    TotalCost = Math.Abs(req.QuantityChange) * req.UnitCost,
                    MovementDate = DateTime.UtcNow,
                    MovementType = req.ReferenceType,
                    ReferenceType = req.ReferenceType,
                    ReferenceId = req.ReferenceId,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.StockMovements.Add(movement);
            }

            return ServiceResult.Ok("Movements applied in context.");
        }

        public async Task<ServiceResult<List<StockBalanceDto>>> GetStockBalancesAsync(Guid? locationId, int page = 1, int pageSize = 50)
        {
            var productQuery = _db.Products.Where(p => !p.IsDeleted).AsQueryable();

            // We want to return all products, so we start with Products table
            var balances = await productQuery
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    Product = p,
                    // Get the balance for this specific location if it exists
                    Balance = locationId.HasValue 
                        ? p.StockBalances.FirstOrDefault(b => b.LocationId == locationId.Value)
                        : null
                })
                .Select(x => new StockBalanceDto
                {
                    LocationId = locationId ?? Guid.Empty,
                    ProductId = x.Product.ProductId,
                    ProductName = x.Product.ProductName,
                    ProductCode = x.Product.ProductCode,
                    QuantityOnHand = x.Balance != null ? x.Balance.QuantityOnHand : 0,
                    AverageCost = x.Balance != null ? x.Balance.AverageCost : 0,
                    LastMovementAt = x.Balance != null ? x.Balance.LastMovementAt : null
                }).ToListAsync();

            return ServiceResult<List<StockBalanceDto>>.Ok(balances);
        }

        public async Task<ServiceResult<List<StockMovementDto>>> GetStockMovementsAsync(
            Guid? locationId, Guid? productId = null, int page = 1, int pageSize = 50)
        {
            var query = _db.StockMovements.AsQueryable();

            if (locationId.HasValue)
                query = query.Where(m => m.LocationId == locationId.Value);

            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId.Value);

            var movements = await query
                .OrderByDescending(m => m.MovementDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new StockMovementDto
                {
                    StockMovementId = m.StockMovementId,
                    MovementDate = m.MovementDate,
                    LocationId = m.LocationId,
                    ProductId = m.ProductId,
                    MovementType = m.MovementType,
                    QuantityIn = m.QuantityIn,
                    QuantityOut = m.QuantityOut,
                    UnitCost = m.UnitCost,
                    TotalCost = m.TotalCost,
                    ReferenceType = m.ReferenceType,
                    ReferenceId = m.ReferenceId,
                    CreatedAt = m.CreatedAt
                }).ToListAsync();

            return ServiceResult<List<StockMovementDto>>.Ok(movements);
        }

        public async Task<ServiceResult> SetInitialStockAsync(InitialStockDto dto, Guid createdByUserId)
        {
            // 1. Validation: Ensure no movements exist for this product at this location
            var hasMovements = await _db.StockMovements
                .AnyAsync(m => m.LocationId == dto.LocationId && m.ProductId == dto.ProductId && !m.IsDeleted);
            
            if (hasMovements)
                return ServiceResult.Fail("Initial stock can only be set once and for products that have no transaction history.");

            // 2. Perform the movement
            var result = await ApplyStockMovementAsync(
                dto.LocationId, 
                dto.ProductId, 
                dto.Quantity, 
                dto.UnitCost, 
                "InitialBalance", 
                Guid.Empty, 
                createdByUserId);

            if (result.Success)
            {
                await _db.SaveChangesAsync();
            }

            return result;
        }
    }
}
