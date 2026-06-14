using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface ITransferService
    {
        Task<ServiceResult<List<TransferRequestDto>>> GetTransfersAsync(Guid locationId, int page = 1, int pageSize = 50);
        Task<ServiceResult<TransferRequestDto>> CreateTransferRequestAsync(TransferRequestCreateDto dto, Guid createdByUserId);
        Task<ServiceResult> AcceptTransferAsync(Guid transferId, Guid userId);
        Task<ServiceResult> RejectTransferAsync(Guid transferId, string reason, Guid userId);
        Task<ServiceResult> PrepareTransferAsync(Guid transferId, Guid userId);
        Task<ServiceResult> ShipTransferAsync(Guid transferId, Guid userId);
        Task<ServiceResult> ReceiveTransferAsync(Guid transferId, Guid userId);
        Task<ServiceResult> RejectReceiptAsync(Guid transferId, string reason, Guid userId);
    }
}
