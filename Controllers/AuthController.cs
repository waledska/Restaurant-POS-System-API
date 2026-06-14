using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApisApp.DTOs.Auth;
using WebApisApp.Helpers;
using WebApisApp.Services;

namespace WebApisApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Validation failed."));

            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data!, result.Message));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            if (!result.Success)
                return Unauthorized(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data!, result.Message));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid user token."));

            // Extract JTI and Expiration to blacklist the access token
            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;
            DateTime? expiresAt = null;

            if (long.TryParse(expClaim, out long unixTime))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            }

            var result = await _authService.LogoutAsync(userId.Value, jti, expiresAt);
            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Success)
                return StatusCode(500, ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyOtpAsync(dto);
            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse.Ok(result.Message));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse.Ok(result.Message));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid user token."));

            var result = await _authService.ChangePasswordAsync(userId.Value, dto);
            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse.Ok(result.Message));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid user token."));

            var result = await _authService.GetMeAsync(userId.Value);
            if (!result.Success)
                return NotFound(ApiResponse.Fail(result.Message, result.Errors));

            return Ok(ApiResponse<object>.Ok(result.Data!));
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
