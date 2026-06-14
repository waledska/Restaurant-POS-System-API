using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IPurchaseService
    {
        Task<ServiceResult<List<PurchaseInvoiceDto>>> GetInvoicesAsync(PurchaseInvoiceFilterDto filter);
        Task<ServiceResult<PurchaseInvoiceDto>> GetInvoiceByIdAsync(Guid id);
        Task<ServiceResult<PurchaseInvoiceDto>> CreateInvoiceAsync(PurchaseInvoiceCreateDto dto, Guid createdByUserId);
        Task<ServiceResult> ApproveInvoiceAsync(Guid invoiceId, Guid approvedByUserId);
        Task<ServiceResult<SupplierPaymentDto>> AddPaymentAsync(SupplierPaymentCreateDto dto, Guid createdByUserId);
        Task<ServiceResult<List<SupplierPaymentDto>>> GetSupplierPaymentsAsync(Guid supplierId);
    }
}
