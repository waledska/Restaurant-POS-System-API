using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IUserService
    {
        Task<ServiceResult<List<UserDto>>> GetAllUsersAsync();
        Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id);
        Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto);
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto);
        Task<ServiceResult> ToggleActivationAsync(Guid id);
        Task<ServiceResult> ResetUserPasswordAsync(Guid userId, string newPassword);
    }
}
