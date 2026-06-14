using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class SyncService : ISyncService
    {
        private readonly ApplicationDbContext _db;

        public SyncService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<SyncPullResponseDto>> PullChangesAsync(SyncPullRequestDto request)
        {
            var latestVersion = await _db.ServerChangeLog.AnyAsync() 
                ? await _db.ServerChangeLog.MaxAsync(x => x.ChangeVersion) 
                : 0;

            // Fetch changes that apply to this location globally (LocationId == null) or specifically
            var logs = await _db.ServerChangeLog
                .Where(x => x.ChangeVersion > request.LastSyncVersion && (x.LocationId == null || x.LocationId == request.LocationId))
                .OrderBy(x => x.ChangeVersion)
                .ToListAsync();

            var response = new SyncPullResponseDto
            {
                LatestServerVersion = latestVersion,
                Changes = logs.Select(x => new ServerChangeLogDto
                {
                    ChangeVersion = x.ChangeVersion,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    OperationType = x.OperationType
                }).ToList()
            };

            // Retrieve the latest state for these rows
            var groupedLogs = logs.GroupBy(x => x.EntityName);
            foreach(var g in groupedLogs)
            {
                var table = g.Key;
                var ids = g.Select(x => x.EntityId).Distinct().ToList();

                var items = await FetchEntitiesAsJsonAsync(table, ids);
                if (items.Any())
                {
                    response.Data[table] = items;
                }
            }

            return ServiceResult<SyncPullResponseDto>.Ok(response);
        }

        public async Task<ServiceResult> PushChangesAsync(SyncPushRequestDto request, Guid userId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var kvp in request.Data)
                {
                    var table = kvp.Key;
                    var rows = kvp.Value;

                    foreach(var row in rows)
                    {
                        await UpsertEntityFromJsonAsync(table, row);
                    }
                }

                // The SyncChangeInterceptor will automatically handle ServerChangeLog generation 
                // when SaveChangesAsync is called.
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Push synchronized successfully.");
            }
            catch(Exception ex)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail($"Failed to process push sync: {ex.Message}");
            }
        }

        private async Task UpsertEntityFromJsonAsync(string entityName, JsonElement json)
        {
            // Generic dynamic upsert based on entity name
            // For a production app, we would use Reflection and DbContext.Set(type)
            // Here we implement the most critical business entities to demonstrate the logic.

            switch (entityName)
            {
                case "Product":
                    var prod = JsonSerializer.Deserialize<Product>(json.GetRawText());
                    if (prod != null)
                    {
                        var existing = await _db.Products.FindAsync(prod.ProductId);
                        if (existing == null) _db.Products.Add(prod);
                        else _db.Entry(existing).CurrentValues.SetValues(prod);
                    }
                    break;

                case "Supplier":
                    var supp = JsonSerializer.Deserialize<Supplier>(json.GetRawText());
                    if (supp != null)
                    {
                        var existing = await _db.Suppliers.FindAsync(supp.SupplierId);
                        if (existing == null) _db.Suppliers.Add(supp);
                        else _db.Entry(existing).CurrentValues.SetValues(supp);
                    }
                    break;

                case "PurchaseInvoice":
                    var inv = JsonSerializer.Deserialize<PurchaseInvoice>(json.GetRawText());
                    if (inv != null)
                    {
                        var existing = await _db.PurchaseInvoices.FindAsync(inv.PurchaseInvoiceId);
                        if (existing == null) _db.PurchaseInvoices.Add(inv);
                        else _db.Entry(existing).CurrentValues.SetValues(inv);
                    }
                    break;

                case "StockMovement":
                    var move = JsonSerializer.Deserialize<StockMovement>(json.GetRawText());
                    if (move != null)
                    {
                        var existing = await _db.StockMovements.FindAsync(move.StockMovementId);
                        if (existing == null) _db.StockMovements.Add(move);
                        else _db.Entry(existing).CurrentValues.SetValues(move);
                    }
                    break;
                
                case "StockBalance":
                    var bal = JsonSerializer.Deserialize<StockBalance>(json.GetRawText());
                    if (bal != null)
                    {
                        var existing = await _db.StockBalances.FindAsync(bal.StockBalanceId);
                        if (existing == null) _db.StockBalances.Add(bal);
                        else _db.Entry(existing).CurrentValues.SetValues(bal);
                    }
                    break;
                
                // (Extend table cases as needed for all 22 entities)
            }
        }

        private async Task<List<JsonElement>> FetchEntitiesAsJsonAsync(string entityName, List<string> ids)
        {
            var result = new List<JsonElement>();
            try 
            {
                // We fetch the latest state of the requested IDs for the pull operation.
                // Using switch for clarity and type safety.
                object? data = null;

                switch (entityName)
                {
                    case "Product":
                        var pIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.Products.Where(x => pIds.Contains(x.ProductId)).ToListAsync();
                        break;
                    case "Location":
                        var lIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.Locations.Where(x => lIds.Contains(x.LocationId)).ToListAsync();
                        break;
                    case "Supplier":
                        var sIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.Suppliers.Where(x => sIds.Contains(x.SupplierId)).ToListAsync();
                        break;
                    case "PurchaseInvoice":
                        var iIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.PurchaseInvoices.Where(x => iIds.Contains(x.PurchaseInvoiceId)).ToListAsync();
                        break;
                    case "StockBalance":
                        var bIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.StockBalances.Where(x => bIds.Contains(x.StockBalanceId)).ToListAsync();
                        break;
                    case "StockMovement":
                        var mIds = ids.Select(Guid.Parse).ToList();
                        data = await _db.StockMovements.Where(x => mIds.Contains(x.StockMovementId)).ToListAsync();
                        break;
                }

                if (data != null)
                {
                    var jsonString = JsonSerializer.Serialize(data);
                    result = JsonSerializer.Deserialize<List<JsonElement>>(jsonString) ?? new List<JsonElement>();
                }
            }
            catch { /* Ignore invalid formats */ }

            return result;
        }
    }
}
