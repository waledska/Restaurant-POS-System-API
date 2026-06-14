using WebApisApp.DTOs.Auth;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IAuthService
    {

        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
        Task<ServiceResult> LogoutAsync(Guid userId, string? jti = null, DateTime? expiresAt = null);
        Task<bool> IsTokenBlacklistedAsync(string jti);
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ServiceResult> VerifyOtpAsync(VerifyOtpDto dto);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto);
        Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<ServiceResult<object>> GetMeAsync(Guid userId);
    }
}
