using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface ISyncService
    {
        Task<ServiceResult<SyncPullResponseDto>> PullChangesAsync(SyncPullRequestDto request);
        Task<ServiceResult> PushChangesAsync(SyncPushRequestDto request, Guid userId);
    }
}
