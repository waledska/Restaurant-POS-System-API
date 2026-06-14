using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditService _auditService;

        public PurchaseService(ApplicationDbContext db, IInventoryService inventoryService, IAuditService auditService)
        {
            _db = db;
            _inventoryService = inventoryService;
            _auditService = auditService;
        }

        public async Task<ServiceResult<List<PurchaseInvoiceDto>>> GetInvoicesAsync(PurchaseInvoiceFilterDto filter)
        {
            var query = _db.PurchaseInvoices.AsQueryable();

            // 1. Filtering
            if (filter.LocationId.HasValue)
                query = query.Where(i => i.LocationId == filter.LocationId.Value);

            if (filter.SupplierId.HasValue)
                query = query.Where(i => i.SupplierId == filter.SupplierId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(i => i.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.InvoiceNumber))
                query = query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.Contains(filter.InvoiceNumber));

            if (filter.FromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= filter.ToDate.Value);

            // 2. Sorting (Newest first)
            query = query.OrderByDescending(i => i.InvoiceDate);

            // 3. Pagination & Selective Projection (Senior practice: No Items in list view)
            var invoices = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(i => new PurchaseInvoiceDto
                {
                    PurchaseInvoiceId = i.PurchaseInvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierId = i.SupplierId,
                    LocationId = i.LocationId,
                    InvoiceDate = i.InvoiceDate,
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    Status = i.Status,
                    Items = new List<PurchaseInvoiceItemDto>() // Excluded for performance
                }).ToListAsync();

            return ServiceResult<List<PurchaseInvoiceDto>>.Ok(invoices);
        }

        public async Task<ServiceResult<PurchaseInvoiceDto>> GetInvoiceByIdAsync(Guid id)
        {
            var i = await _db.PurchaseInvoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.PurchaseInvoiceId == id);

            if (i == null) return ServiceResult<PurchaseInvoiceDto>.Fail("Invoice not found.");

            var dto = new PurchaseInvoiceDto
            {
                PurchaseInvoiceId = i.PurchaseInvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                SupplierId = i.SupplierId,
                LocationId = i.LocationId,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                Status = i.Status,
                Items = i.Items.Select(item => new PurchaseInvoiceItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.LineTotal
                }).ToList()
            };

            return ServiceResult<PurchaseInvoiceDto>.Ok(dto);
        }

        public async Task<ServiceResult<PurchaseInvoiceDto>> CreateInvoiceAsync(PurchaseInvoiceCreateDto dto, Guid createdByUserId)
        {
            if (dto.Items == null || !dto.Items.Any())
                return ServiceResult<PurchaseInvoiceDto>.Fail("Invoice must contain at least one item.");

            var loc = await _db.Locations.FirstOrDefaultAsync(l => l.LocationId == dto.LocationId && !l.IsDeleted);
            if (loc == null) 
                return ServiceResult<PurchaseInvoiceDto>.Fail($"Location ID {dto.LocationId} not found or is deleted.");
            
            if (loc.LocationType != "Warehouse")
                return ServiceResult<PurchaseInvoiceDto>.Fail($"Location '{loc.LocationName}' is not a Warehouse. Purchases can only be received in warehouse locations.");

            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == dto.SupplierId && !s.IsDeleted);
            if (supplier == null)
                return ServiceResult<PurchaseInvoiceDto>.Fail($"Supplier ID {dto.SupplierId} not found or is inactive.");

            // Validate all products and item details
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var existingProducts = await _db.Products
                .Where(p => productIds.Contains(p.ProductId) && !p.IsDeleted)
                .Select(p => p.ProductId)
                .ToListAsync();

            if (existingProducts.Count != productIds.Count)
            {
                var missingIds = productIds.Except(existingProducts).ToList();
                return ServiceResult<PurchaseInvoiceDto>.Fail($"The following Product IDs do not exist or are deleted: {string.Join(", ", missingIds)}");
            }

            // Validate Quantity and UnitPrice
            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return ServiceResult<PurchaseInvoiceDto>.Fail($"Invalid quantity for product {item.ProductId}. Quantity must be greater than zero.");
                if (item.UnitPrice < 0)
                    return ServiceResult<PurchaseInvoiceDto>.Fail($"Invalid unit price for product {item.ProductId}. Unit price cannot be negative.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var invoice = new PurchaseInvoice
                {
                    PurchaseInvoiceId = Guid.NewGuid(),
                    InvoiceNumber = $"PI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    SupplierId = dto.SupplierId,
                    LocationId = dto.LocationId,
                    InvoiceDate = DateTime.UtcNow,
                    Status = "Draft",
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = dto.Items.Sum(x => x.Quantity * x.UnitPrice),
                    PaidAmount = 0,
                    RemainingAmount = dto.Items.Sum(x => x.Quantity * x.UnitPrice)
                };

                foreach (var item in dto.Items)
                {
                    invoice.Items.Add(new PurchaseInvoiceItem
                    {
                        PurchaseInvoiceItemId = Guid.NewGuid(),
                        PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.Quantity * item.UnitPrice
                    });
                }

                _db.PurchaseInvoices.Add(invoice);
                await _db.SaveChangesAsync(); // Save to get the relations/items in DB context

                await _auditService.LogActionAsync(createdByUserId, "CreateInvoice", "PurchaseInvoices", invoice.PurchaseInvoiceId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetInvoiceByIdAsync(invoice.PurchaseInvoiceId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ServiceResult<PurchaseInvoiceDto>.Fail($"Failed to create invoice: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ApproveInvoiceAsync(Guid invoiceId, Guid approvedByUserId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try 
            {
                var invoice = await _db.PurchaseInvoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.PurchaseInvoiceId == invoiceId);
                if (invoice == null) return ServiceResult.Fail("Invoice not found.");
                if (invoice.Status != "Draft") return ServiceResult.Fail("Invoice already processed.");

                var supplier = await _db.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier == null) return ServiceResult.Fail("Supplier not found.");

                // Map to Stock Movements
                var movements = invoice.Items.Select(item => new StockMovementRequest
                {
                    LocationId = invoice.LocationId,
                    ProductId = item.ProductId,
                    QuantityChange = item.Quantity,
                    UnitCost = item.UnitPrice,
                    ReferenceType = "PurchaseInvoice",
                    ReferenceId = invoice.PurchaseInvoiceId
                }).ToList();

                var movementResult = await _inventoryService.ApplyStockMovementBatchAsync(movements, approvedByUserId);
                if (!movementResult.Success) throw new Exception(movementResult.Message);

                // Update supplier balance
                supplier.CurrentBalance += invoice.RemainingAmount;

                invoice.Status = "Approved";

                await _auditService.LogActionAsync(approvedByUserId, "ApproveInvoice", "PurchaseInvoices", invoice.PurchaseInvoiceId.ToString());
                
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Invoice approved and inventory updated successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail($"Failed to approve invoice: {ex.Message}");
            }
        }

        public async Task<ServiceResult<SupplierPaymentDto>> AddPaymentAsync(SupplierPaymentCreateDto dto, Guid createdByUserId)
        {
            // 1. Basic Validation
            if (dto.Amount <= 0)
                return ServiceResult<SupplierPaymentDto>.Fail("Payment amount must be greater than zero.");

            // 2. Entity Existence Checks
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == dto.SupplierId && !s.IsDeleted);
            if (supplier == null)
                return ServiceResult<SupplierPaymentDto>.Fail($"Supplier ID {dto.SupplierId} not found or is inactive.");

            var paymentMethod = await _db.SystemPaymentMethods.FirstOrDefaultAsync(m => m.SystemPaymentMethodId == dto.SystemPaymentMethodId && !m.IsDeleted);
            if (paymentMethod == null)
                return ServiceResult<SupplierPaymentDto>.Fail($"Payment Method ID {dto.SystemPaymentMethodId} not found or is inactive.");

            PurchaseInvoice? invoice = null;
            if (dto.PurchaseInvoiceId.HasValue)
            {
                invoice = await _db.PurchaseInvoices.FirstOrDefaultAsync(i => i.PurchaseInvoiceId == dto.PurchaseInvoiceId.Value);
                if (invoice == null)
                    return ServiceResult<SupplierPaymentDto>.Fail($"Purchase Invoice ID {dto.PurchaseInvoiceId} not found.");

                if (invoice.SupplierId != dto.SupplierId)
                    return ServiceResult<SupplierPaymentDto>.Fail($"Security Alert: The specified invoice belongs to a different supplier.");

                if (invoice.RemainingAmount == 0 || invoice.Status == "Paid")
                    return ServiceResult<SupplierPaymentDto>.Fail("This invoice is already fully paid.");
                
                if (dto.Amount > invoice.RemainingAmount)
                    return ServiceResult<SupplierPaymentDto>.Fail($"Payment amount ({dto.Amount}) cannot be greater than the remaining invoice amount ({invoice.RemainingAmount}).");
            }
            else
            {
                if (supplier.CurrentBalance <= 0)
                    return ServiceResult<SupplierPaymentDto>.Fail("The supplier does not have any outstanding balance to pay.");
                
                if (dto.Amount > supplier.CurrentBalance)
                    return ServiceResult<SupplierPaymentDto>.Fail($"Payment amount ({dto.Amount}) cannot be greater than the overall supplier balance ({supplier.CurrentBalance}).");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var payment = new SupplierPayment
                {
                    SupplierPaymentId = Guid.NewGuid(),
                    SupplierId = dto.SupplierId,
                    PurchaseInvoiceId = dto.PurchaseInvoiceId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = dto.Amount,
                    SystemPaymentMethodId = dto.SystemPaymentMethodId,
                    CreatedByUserId = createdByUserId,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                // Reduce supplier balance
                supplier.CurrentBalance -= dto.Amount;

                // Adjust invoice if applicable
                if (invoice != null)
                {
                    invoice.PaidAmount += dto.Amount;
                    invoice.RemainingAmount -= dto.Amount;
                    
                    if (invoice.RemainingAmount <= 0)
                    {
                        invoice.RemainingAmount = 0;
                        invoice.Status = "Paid";
                    }
                    else
                    {
                        invoice.Status = "PartiallyPaid";
                    }
                }

                _db.SupplierPayments.Add(payment);
                
                await _auditService.LogActionAsync(createdByUserId, "AddPayment", "SupplierPayments", payment.SupplierPaymentId.ToString());
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult<SupplierPaymentDto>.Ok(new SupplierPaymentDto
                {
                    SupplierPaymentId = payment.SupplierPaymentId,
                    SupplierId = payment.SupplierId,
                    PurchaseInvoiceId = payment.PurchaseInvoiceId,
                    PaymentDate = payment.PaymentDate,
                    Amount = payment.Amount,
                    SystemPaymentMethodId = payment.SystemPaymentMethodId,
                    PaymentMethodType = paymentMethod.MethodType,
                    PaymentMethodDetails = paymentMethod.AccountData,
                    Notes = payment.Notes,
                    CreatedAt = payment.CreatedAt
                }, "Payment recorded successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ServiceResult<SupplierPaymentDto>.Fail($"Database operation failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<SupplierPaymentDto>>> GetSupplierPaymentsAsync(Guid supplierId)
        {
            var payments = await _db.SupplierPayments
                .Include(p => p.SystemPaymentMethod)
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new SupplierPaymentDto
                {
                    SupplierPaymentId = p.SupplierPaymentId,
                    SupplierId = p.SupplierId,
                    PurchaseInvoiceId = p.PurchaseInvoiceId,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    SystemPaymentMethodId = p.SystemPaymentMethodId,
                    PaymentMethodType = p.SystemPaymentMethod.MethodType,
                    PaymentMethodDetails = p.SystemPaymentMethod.AccountData,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt
                }).ToListAsync();

            return ServiceResult<List<SupplierPaymentDto>>.Ok(payments);
        }
    }
}
