using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class StockCountService : IStockCountService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditService _auditService;

        public StockCountService(ApplicationDbContext db, IInventoryService inventoryService, IAuditService auditService)
        {
            _db = db;
            _inventoryService = inventoryService;
            _auditService = auditService;
        }

        public async Task<ServiceResult<List<StockCountDto>>> GetCountsAsync(Guid locationId, int page = 1, int pageSize = 50)
        {
            var locationExists = await _db.Locations.AnyAsync(l => l.LocationId == locationId && !l.IsDeleted);
            if (!locationExists)
                return ServiceResult<List<StockCountDto>>.Fail($"Location ID {locationId} not found.");

            var list = await _db.StockCounts
                .Where(c => c.LocationId == locationId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new StockCountDto
                {
                    StockCountId = c.StockCountId,
                    LocationId = c.LocationId,
                    CountDate = c.CountDate,
                    Status = c.Status,
                    Notes = c.Notes
                }).ToListAsync();

            return ServiceResult<List<StockCountDto>>.Ok(list);
        }

        public async Task<ServiceResult<StockCountDto>> CreateStockCountAsync(StockCountCreateDto dto, Guid createdByUserId)
        {
            // 1. Validation: Verify all products exist and are not deleted
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var validProducts = await _db.Products
                .Where(p => productIds.Contains(p.ProductId) && !p.IsDeleted)
                .Select(p => p.ProductId)
                .ToListAsync();

            if (validProducts.Count != productIds.Count)
            {
                var invalidIds = productIds.Except(validProducts).ToList();
                return ServiceResult<StockCountDto>.Fail($"Invalid or non-existent Product IDs detected: {string.Join(", ", invalidIds)}");
            }

            var sc = new StockCount
            {
                StockCountId = Guid.NewGuid(),
                LocationId = dto.LocationId,
                CountDate = DateTime.UtcNow,
                Status = "قيد الانتظار",
                Notes = dto.Notes,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            foreach(var item in dto.Items)
            {
                var bal = await _db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == item.ProductId && b.LocationId == dto.LocationId);
                var sysQty = bal?.QuantityOnHand ?? 0;

                sc.Items.Add(new StockCountItem
                {
                    StockCountItemId = Guid.NewGuid(),
                    StockCountId = sc.StockCountId,
                    ProductId = item.ProductId,
                    SystemQty = sysQty,
                    ActualQty = item.ActualQuantity,
                    DifferenceQty = item.ActualQuantity.HasValue ? (item.ActualQuantity.Value - sysQty) : null
                });
            }

            _db.StockCounts.Add(sc);
            await _auditService.LogActionAsync(createdByUserId, "CreateStockCount", "StockCounts", sc.StockCountId.ToString());
            await _db.SaveChangesAsync();

            return ServiceResult<StockCountDto>.Ok(new StockCountDto
            {
                StockCountId = sc.StockCountId,
                LocationId = sc.LocationId,
                CountDate = sc.CountDate,
                Status = sc.Status
            });
        }

        public async Task<ServiceResult<StockCountDetailDto>> GetCountByIdAsync(Guid stockCountId)
        {
            var sc = await _db.StockCounts
                .Include(x => x.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(x => x.StockCountId == stockCountId);

            if (sc == null) return ServiceResult<StockCountDetailDto>.Fail("Stock count not found.");

            var detail = new StockCountDetailDto
            {
                StockCountId = sc.StockCountId,
                LocationId = sc.LocationId,
                CountDate = sc.CountDate,
                Status = sc.Status,
                Notes = sc.Notes,
                Items = sc.Items.Select(i => new StockCountItemDto
                {
                    StockCountItemId = i.StockCountItemId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    SystemQty = i.SystemQty,
                    ActualQty = i.ActualQty,
                    DifferenceQty = i.DifferenceQty
                }).ToList()
            };

            return ServiceResult<StockCountDetailDto>.Ok(detail);
        }

        public async Task<ServiceResult> PostStockCountAsync(Guid stockCountId, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try {
                var sc = await _db.StockCounts.Include(x => x.Items).FirstOrDefaultAsync(x => x.StockCountId == stockCountId);
                if (sc == null) return ServiceResult.Fail("Stock count not found.");
                if (sc.Status != "قيد الانتظار") return ServiceResult.Fail("Only pending stock counts can be posted.");

                var movements = new List<StockMovementRequest>();
                foreach (var item in sc.Items)
                {
                    var diff = item.ActualQty.HasValue ? (item.ActualQty.Value - item.SystemQty) : 0m;
                    
                    if (diff != 0)
                    {
                        var bal = await _db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == item.ProductId && b.LocationId == sc.LocationId);
                        var cost = bal?.AverageCost ?? 0;

                        movements.Add(new StockMovementRequest
                        {
                            LocationId = sc.LocationId,
                            ProductId = item.ProductId,
                            QuantityChange = diff,
                            UnitCost = cost,
                            ReferenceType = "StockCountAdjustment",
                            ReferenceId = sc.StockCountId
                        });
                    }
                }

                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, userId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                sc.Status = "معتمد";
                sc.PostedAt = DateTime.UtcNow;
                await _auditService.LogActionAsync(userId, "PostStockCount", "StockCounts", stockCountId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Stock count posted and variances adjusted.");
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteStockCountAsync(Guid stockCountId, Guid userId)
        {
            var sc = await _db.StockCounts.Include(x => x.Items).FirstOrDefaultAsync(x => x.StockCountId == stockCountId);
            if (sc == null) return ServiceResult.Fail("Stock count not found.");
            if (sc.Status != "قيد الانتظار") return ServiceResult.Fail("Only pending stock counts can be deleted.");

            // We soft-delete by setting IsDeleted = true (Inherited from BaseEntity)
            sc.IsDeleted = true;
            foreach (var item in sc.Items)
            {
                item.IsDeleted = true;
            }

            await _auditService.LogActionAsync(userId, "DeleteStockCount", "StockCounts", stockCountId.ToString());
            await _db.SaveChangesAsync();

            return ServiceResult.Ok("Stock count deleted successfully.");
        }
    }
}
