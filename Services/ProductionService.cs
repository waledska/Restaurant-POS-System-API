using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class ProductionService : IProductionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditService _auditService;

        public ProductionService(ApplicationDbContext db, IInventoryService inventoryService, IAuditService auditService)
        {
            _db = db;
            _inventoryService = inventoryService;
            _auditService = auditService;
        }

        public async Task<ServiceResult<List<ProductionOperationDto>>> GetProductionsAsync(Guid locationId, int page = 1, int pageSize = 50)
        {
            var locationExists = await _db.Locations.AnyAsync(l => l.LocationId == locationId && !l.IsDeleted);
            if (!locationExists)
                return ServiceResult<List<ProductionOperationDto>>.Fail($"Location ID {locationId} not found.");

            var list = await _db.ProductionOperations
                .Where(p => p.LocationId == locationId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductionOperationDto
                {
                    ProductionOperationId = p.ProductionOperationId,
                    LocationId = p.LocationId,
                    ManufacturedProductId = p.ManufacturedProductId,
                    QuantityProduced = p.QuantityProduced,
                    TotalCost = p.TotalCost,
                    ProductionDate = p.ProductionDate
                }).ToListAsync();

            return ServiceResult<List<ProductionOperationDto>>.Ok(list);
        }

        public async Task<ServiceResult<ProductionOperationDto>> CreateProductionAsync(ProductionCreateDto dto, Guid createdByUserId)
        {
            // 1. Basic Validation
            if (dto.QuantityProduced <= 0)
                return ServiceResult<ProductionOperationDto>.Fail("Quantity produced must be greater than zero.");

            var location = await _db.Locations.FirstOrDefaultAsync(l => l.LocationId == dto.LocationId && !l.IsDeleted);
            if (location == null)
                return ServiceResult<ProductionOperationDto>.Fail($"Location ID {dto.LocationId} not found or is inactive.");

            var targetProduct = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == dto.ManufacturedProductId && !p.IsDeleted);
            if (targetProduct == null)
                return ServiceResult<ProductionOperationDto>.Fail($"Product ID {dto.ManufacturedProductId} not found.");

            if (targetProduct.ProductType != "Manufactured")
                return ServiceResult<ProductionOperationDto>.Fail($"Invalid operation: Product '{targetProduct.ProductName}' is of type '{targetProduct.ProductType}' and cannot be manufactured. Only 'Manufactured' products are allowed.");

            var recipes = await _db.ProductRecipes
                .Where(r => r.ManufacturedProductId == dto.ManufacturedProductId && !r.IsDeleted)
                .ToListAsync();

            if (!recipes.Any())
                return ServiceResult<ProductionOperationDto>.Fail($"Production failed: Product '{targetProduct.ProductName}' has no recipe items defined. Please add raw materials to its recipe first.");

            using var tx = await _db.Database.BeginTransactionAsync();
            try {
                var op = new ProductionOperation
                {
                    ProductionOperationId = Guid.NewGuid(),
                    LocationId = dto.LocationId,
                    ManufacturedProductId = dto.ManufacturedProductId,
                    QuantityProduced = dto.QuantityProduced,
                    TotalCost = 0,
                    ProductionDate = DateTime.UtcNow,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                var movements = new List<StockMovementRequest>();
                decimal totalCost = 0;

                foreach(var r in recipes)
                {
                    var qtyToConsume = r.QuantityNeeded * dto.QuantityProduced;
                    var bal = await _db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == r.RawProductId && b.LocationId == dto.LocationId);
                    if (bal == null || bal.QuantityOnHand < qtyToConsume)
                        return ServiceResult<ProductionOperationDto>.Fail($"Insufficient raw materials for Product {r.RawProductId}");

                    var cost = bal.AverageCost;
                    totalCost += (qtyToConsume * cost);

                    op.Items.Add(new ProductionOperationItem
                    {
                        ProductionOperationItemId = Guid.NewGuid(),
                        ProductionOperationId = op.ProductionOperationId,
                        RawProductId = r.RawProductId,
                        QuantityConsumed = qtyToConsume,
                        UnitCost = cost,
                        TotalCost = qtyToConsume * cost
                    });

                    movements.Add(new StockMovementRequest
                    {
                        LocationId = op.LocationId,
                        ProductId = r.RawProductId,
                        QuantityChange = -qtyToConsume,
                        UnitCost = cost,
                        ReferenceType = "ProductionConsume",
                        ReferenceId = op.ProductionOperationId
                    });
                }

                op.TotalCost = totalCost;
                decimal unitMfgCost = totalCost > 0 && op.QuantityProduced > 0 ? (totalCost / op.QuantityProduced) : 0;
                op.UnitCost = unitMfgCost;
                
                movements.Add(new StockMovementRequest
                {
                    LocationId = op.LocationId,
                    ProductId = op.ManufacturedProductId,
                    QuantityChange = op.QuantityProduced,
                    UnitCost = unitMfgCost,
                    ReferenceType = "ProductionIn",
                    ReferenceId = op.ProductionOperationId
                });

                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, createdByUserId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                _db.ProductionOperations.Add(op);
                await _auditService.LogActionAsync(createdByUserId, "CreateProduction", "ProductionOperations", op.ProductionOperationId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult<ProductionOperationDto>.Ok(new ProductionOperationDto
                {
                    ProductionOperationId = op.ProductionOperationId,
                    LocationId = op.LocationId,
                    ManufacturedProductId = op.ManufacturedProductId,
                    QuantityProduced = op.QuantityProduced,
                    TotalCost = op.TotalCost,
                    ProductionDate = op.ProductionDate
                });
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return ServiceResult<ProductionOperationDto>.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteProductionAsync(Guid productionId, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var op = await _db.ProductionOperations
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.ProductionOperationId == productionId && !p.IsDeleted);

                if (op == null) return ServiceResult.Fail("Production operation not found.");

                // 1. Safety Check: Verify enough Manufactured Product is available to reverse
                var mBalance = await _db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == op.ManufacturedProductId && b.LocationId == op.LocationId);
                if (mBalance == null || mBalance.QuantityOnHand < op.QuantityProduced)
                {
                    return ServiceResult.Fail($"Cannot cancel production: Insufficient manufactured product in stock ({op.QuantityProduced} needed, {mBalance?.QuantityOnHand ?? 0} found).");
                }

                var movements = new List<StockMovementRequest>();

                // ─── Reverse manufactured product's AverageCost ──────────────────────
                // ApplyStockMovementBatchAsync only recalculates AverageCost on inbound
                // (QuantityChange > 0). For outbound ProductionCancelOut we must pre-set
                // the restored average cost BEFORE calling the batch, using the inverse
                // of the weighted-average formula applied during CreateProductionAsync.
                //
                //   C_restored = (CurrentQty × CurrentAvg − Qp × Cp) / (CurrentQty − Qp)
                //
                decimal mfgNewQty = mBalance.QuantityOnHand - op.QuantityProduced;
                if (mfgNewQty > 0)
                {
                    decimal restoredCost =
                        (mBalance.QuantityOnHand * mBalance.AverageCost
                         - op.QuantityProduced * op.UnitCost)
                        / mfgNewQty;

                    mBalance.AverageCost = restoredCost >= 0 ? restoredCost : 0;
                }
                else
                {
                    // All manufactured stock will be gone — reset cost to 0
                    mBalance.AverageCost = 0;
                }
                // QuantityOnHand will be decremented by ApplyStockMovementBatchAsync below.

                // 2. Reverse Manufactured Product (Outbound)
                movements.Add(new StockMovementRequest
                {
                    LocationId = op.LocationId,
                    ProductId = op.ManufacturedProductId,
                    QuantityChange = -op.QuantityProduced,
                    UnitCost = op.UnitCost,
                    ReferenceType = "ProductionCancelOut",
                    ReferenceId = op.ProductionOperationId
                });

                // 3. Reverse Raw Materials (Return to stock)
                foreach (var item in op.Items)
                {
                    movements.Add(new StockMovementRequest
                    {
                        LocationId = op.LocationId,
                        ProductId = item.RawProductId,
                        QuantityChange = item.QuantityConsumed,
                        UnitCost = item.UnitCost,
                        ReferenceType = "ProductionCancelReturn",
                        ReferenceId = op.ProductionOperationId
                    });
                    item.IsDeleted = true;
                }

                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, userId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                // 4. Mark as Deleted
                op.IsDeleted = true;
                await _auditService.LogActionAsync(userId, "DeleteProduction", "ProductionOperations", op.ProductionOperationId.ToString());
                
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Production canceled and stock reversed successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
        }
    }
}
