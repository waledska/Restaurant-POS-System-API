using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Hubs;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditService _auditService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TransferService(
            ApplicationDbContext db, 
            IInventoryService inventoryService, 
            IAuditService auditService,
            IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _inventoryService = inventoryService;
            _auditService = auditService;
            _hubContext = hubContext;
        }

        private async Task NotifyLocationAsync(Guid locationId, string message)
        {
            await _hubContext.Clients.Group($"Location_{locationId}").SendAsync("ReceiveNotification", message);
        }

        public async Task<ServiceResult<List<TransferRequestDto>>> GetTransfersAsync(Guid locationId, int page = 1, int pageSize = 50)
        {
            var list = await _db.TransferRequests
                .Include(t => t.Items)
                .Where(t => t.FromLocationId == locationId || t.ToLocationId == locationId)
                .OrderByDescending(t => t.RequestDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransferRequestDto
                {
                    TransferRequestId = t.TransferRequestId,
                    TransferCode = t.TransferCode,
                    FromLocationId = t.FromLocationId,
                    ToLocationId = t.ToLocationId,
                    RequestDate = t.RequestDate,
                    Status = t.Status,
                    RejectionReason = t.RejectedReason,
                    PreparedAt = t.PreparedAt,
                    ShippedAt = t.ShippedAt,
                    ReceivedAt = t.ReceivedAt,
                    Items = t.Items.Select(i => new TransferRequestItemDto
                    {
                        ProductId = i.ProductId,
                        RequestedQuantity = i.RequestedQty
                    }).ToList()
                }).ToListAsync();

            return ServiceResult<List<TransferRequestDto>>.Ok(list);
        }

        public async Task<ServiceResult<TransferRequestDto>> CreateTransferRequestAsync(TransferRequestCreateDto dto, Guid createdByUserId)
        {
            if (dto.FromLocationId == dto.ToLocationId)
                return ServiceResult<TransferRequestDto>.Fail("Source and Destination locations cannot be the same.");

            var locationsExist = await _db.Locations.CountAsync(l => l.LocationId == dto.FromLocationId || l.LocationId == dto.ToLocationId);
            if (locationsExist < (dto.FromLocationId == dto.ToLocationId ? 1 : 2))
                return ServiceResult<TransferRequestDto>.Fail("One or both specified Location IDs do not exist.");

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var validProductsCount = await _db.Products.CountAsync(p => productIds.Contains(p.ProductId));
            if (validProductsCount != productIds.Count)
                return ServiceResult<TransferRequestDto>.Fail("One or more specified Product IDs do not exist or are invalid.");

            var req = new TransferRequest
            {
                TransferRequestId = Guid.NewGuid(),
                TransferCode = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100,999)}",
                FromLocationId = dto.FromLocationId,
                ToLocationId = dto.ToLocationId,
                RequestDate = DateTime.UtcNow,
                Status = "Pending",
                RequestedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            foreach(var item in dto.Items)
            {
                req.Items.Add(new TransferRequestItem
                {
                    TransferRequestItemId = Guid.NewGuid(),
                    TransferRequestId = req.TransferRequestId,
                    ProductId = item.ProductId,
                    RequestedQty = item.RequestedQuantity
                });
            }

            _db.TransferRequests.Add(req);
            await _auditService.LogActionAsync(createdByUserId, "CreateTransfer", "TransferRequests", req.TransferRequestId.ToString());
            await _db.SaveChangesAsync();

            await NotifyLocationAsync(dto.FromLocationId, $"New transfer request {req.TransferCode} from Location {dto.ToLocationId}");

            return ServiceResult<TransferRequestDto>.Ok(new TransferRequestDto { TransferRequestId = req.TransferRequestId, TransferCode = req.TransferCode, Status = req.Status });
        }

        public async Task<ServiceResult> AcceptTransferAsync(Guid transferId, Guid userId)
        {
            var t = await _db.TransferRequests.FindAsync(transferId);
            if (t == null) return ServiceResult.Fail("Transfer not found.");
            if (t.Status != "Pending") return ServiceResult.Fail("Only pending transfers can be accepted.");

            t.Status = "Accepted";
            await _auditService.LogActionAsync(userId, "AcceptTransfer", "TransferRequests", transferId.ToString());
            await _db.SaveChangesAsync();

            await NotifyLocationAsync(t.ToLocationId, $"Transfer {t.TransferCode} was Accepted.");
            return ServiceResult.Ok("Transfer accepted.");
        }

        public async Task<ServiceResult> RejectTransferAsync(Guid transferId, string reason, Guid userId)
        {
            var t = await _db.TransferRequests.FindAsync(transferId);
            if (t == null) return ServiceResult.Fail("Transfer not found.");
            if (t.Status != "Pending") return ServiceResult.Fail("Only pending transfers can be rejected.");

            t.Status = "Rejected";
            t.RejectedReason = reason;

            await _auditService.LogActionAsync(userId, "RejectTransfer", "TransferRequests", transferId.ToString(), reason);
            await _db.SaveChangesAsync();

            await NotifyLocationAsync(t.ToLocationId, $"Transfer {t.TransferCode} was Rejected: {reason}");
            return ServiceResult.Ok("Transfer rejected.");
        }

        public async Task<ServiceResult> PrepareTransferAsync(Guid transferId, Guid userId)
        {
            var t = await _db.TransferRequests.FindAsync(transferId);
            if (t == null) return ServiceResult.Fail("Transfer not found.");
            if (t.Status != "Accepted") return ServiceResult.Fail("Only accepted transfers can be prepared.");

            t.Status = "Preparing";
            t.PreparedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(userId, "PrepareTransfer", "TransferRequests", transferId.ToString());
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Transfer preparing.");
        }

        public async Task<ServiceResult> ShipTransferAsync(Guid transferId, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try {
                var t = await _db.TransferRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.TransferRequestId == transferId);
                if (t == null) return ServiceResult.Fail("Transfer not found.");
                if (t.Status != "Preparing") return ServiceResult.Fail("Only preparing transfers can be shipped.");

                t.Status = "InTransit";
                t.ShippedAt = DateTime.UtcNow;

                // Create movements out of Source Location
                var movements = new List<StockMovementRequest>();
                foreach (var item in t.Items)
                {
                    // get avg cost
                    var bal = await _db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == item.ProductId && b.LocationId == t.FromLocationId);
                    var cost = bal?.AverageCost ?? 0;

                    movements.Add(new StockMovementRequest
                    {
                        LocationId = t.FromLocationId,
                        ProductId = item.ProductId,
                        QuantityChange = -item.RequestedQty, // deduct
                        UnitCost = cost,
                        ReferenceType = "TransferOut",
                        ReferenceId = t.TransferRequestId
                    });
                }
                
                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, userId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                await _auditService.LogActionAsync(userId, "ShipTransfer", "TransferRequests", transferId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await NotifyLocationAsync(t.ToLocationId, $"Transfer {t.TransferCode} is In Transit.");
                return ServiceResult.Ok("Transfer shipped.");
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult> ReceiveTransferAsync(Guid transferId, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try {
                var t = await _db.TransferRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.TransferRequestId == transferId);
                if (t == null) return ServiceResult.Fail("Transfer not found.");
                if (t.Status != "InTransit") return ServiceResult.Fail("Only InTransit transfers can be received.");

                t.Status = "Received";
                t.ReceivedAt = DateTime.UtcNow;

                // Create movements into Target Location
                var movements = new List<StockMovementRequest>();
                foreach (var item in t.Items)
                {
                    // To keep cost accurate, we check out-movements or source balance
                    var sourceMove = await _db.StockMovements
                        .FirstOrDefaultAsync(m => m.ReferenceId == t.TransferRequestId && m.ReferenceType == "TransferOut" && m.ProductId == item.ProductId);
                    var cost = sourceMove?.UnitCost ?? 0;

                    movements.Add(new StockMovementRequest
                    {
                        LocationId = t.ToLocationId,
                        ProductId = item.ProductId,
                        QuantityChange = item.RequestedQty, // add
                        UnitCost = cost,
                        ReferenceType = "TransferIn",
                        ReferenceId = t.TransferRequestId
                    });
                }
                
                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, userId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                await _auditService.LogActionAsync(userId, "ReceiveTransfer", "TransferRequests", transferId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await NotifyLocationAsync(t.FromLocationId, $"Transfer {t.TransferCode} was Received.");
                return ServiceResult.Ok("Transfer received successfully.");
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
        }

        public async Task<ServiceResult> RejectReceiptAsync(Guid transferId, string reason, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try {
                var t = await _db.TransferRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.TransferRequestId == transferId);
                if (t == null) return ServiceResult.Fail("Transfer not found.");
                if (t.Status != "InTransit") return ServiceResult.Fail("Only InTransit transfers can be rejected on receipt.");

                t.Status = "Rejected";
                t.RejectedReason = $"Rejected on Receipt: {reason}";
                t.ReceivedAt = DateTime.UtcNow;

                // Since it's rejected on receipt, the stock must be returned to the FromLocation
                var movements = new List<StockMovementRequest>();
                foreach (var item in t.Items)
                {
                    var sourceMove = await _db.StockMovements
                        .FirstOrDefaultAsync(m => m.ReferenceId == t.TransferRequestId && m.ReferenceType == "TransferOut" && m.ProductId == item.ProductId);
                    var cost = sourceMove?.UnitCost ?? 0;

                    movements.Add(new StockMovementRequest
                    {
                        LocationId = t.FromLocationId,
                        ProductId = item.ProductId,
                        QuantityChange = item.RequestedQty, // Return stock back to source
                        UnitCost = cost,
                        ReferenceType = "TransferReturn",
                        ReferenceId = t.TransferRequestId
                    });
                }

                var moveRes = await _inventoryService.ApplyStockMovementBatchAsync(movements, userId);
                if (!moveRes.Success) throw new Exception(moveRes.Message);

                await _auditService.LogActionAsync(userId, "RejectReceipt", "TransferRequests", transferId.ToString(), t.RejectedReason);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await NotifyLocationAsync(t.FromLocationId, $"Transfer {t.TransferCode} was Rejected by receiver: {reason}");
                return ServiceResult.Ok("Transfer receipt rejected and stock reversed.");
            } catch (Exception ex) {
                await tx.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
        }
    }
}
