using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Auth;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext db,
            IPasswordHasher<User> passwordHasher,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
        }

        // ── Login (by email OR username) ──────────────────────
        public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email == dto.EmailOrUserName || u.UserName == dto.EmailOrUserName);

            if (user is null || !user.IsActive)
                return ServiceResult<AuthResponseDto>.Fail("Invalid credentials.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return ServiceResult<AuthResponseDto>.Fail("Invalid credentials.");

            user.LastLoginAt = DateTime.UtcNow;

            // Revoke old tokens for this user
            var oldTokens = await _db.RefreshTokens
                .Where(t => t.UserId == user.UserId && t.RevokedAt == null)
                .ToListAsync();
            foreach (var t in oldTokens) t.RevokedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var token = await _jwtService.GenerateTokenAsync(user);
            var refreshToken = await CreateRefreshTokenAsync(user.UserId);

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return ServiceResult<AuthResponseDto>.Ok(BuildAuthResponse(user, token, refreshToken), "Login successful.");
        }

        // ── Refresh Token ─────────────────────────────────────
        public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            var stored = await _db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == refreshToken && t.RevokedAt == null);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return ServiceResult<AuthResponseDto>.Fail("Invalid or expired refresh token.");

            var user = stored.User;
            if (user == null || !user.IsActive)
                return ServiceResult<AuthResponseDto>.Fail("User account is disabled.");

            // Revoke old, issue new
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var newJwt = await _jwtService.GenerateTokenAsync(user);
            var newRefresh = await CreateRefreshTokenAsync(user.UserId);

            return ServiceResult<AuthResponseDto>.Ok(BuildAuthResponse(user, newJwt, newRefresh), "Token refreshed.");
        }

        // ── Logout ────────────────────────────────────────────
        public async Task<ServiceResult> LogoutAsync(Guid userId, string? jti = null, DateTime? expiresAt = null)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(jti) && expiresAt.HasValue)
            {
                var blacklisted = new BlacklistedToken
                {
                    BlacklistedTokenId = Guid.NewGuid(),
                    Jti = jti,
                    ExpiresAt = expiresAt.Value
                };
                _db.BlacklistedTokens.Add(blacklisted);
            }

            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Logged out successfully.");
        }

        public async Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            if (string.IsNullOrEmpty(jti)) return false;
            return await _db.BlacklistedTokens.AnyAsync(t => t.Jti == jti && t.ExpiresAt > DateTime.UtcNow);
        }

        // ── Forgot Password (OTP) ────────────────────────────
        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null)
                return ServiceResult.Ok("If this email exists, an OTP has been sent.");

            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var resetOtp = new PasswordResetOtp
            {
                PasswordResetOtpId = Guid.NewGuid(),
                UserId = user.UserId,
                CodeHash = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.PasswordResetOtps.Add(resetOtp);
            await _db.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(user.Email!, otp);
            }
            catch (Exception ex)
            {
                _db.PasswordResetOtps.Remove(resetOtp);
                await _db.SaveChangesAsync();
                _logger.LogError(ex, "Failed to send OTP email to {Email}", user.Email);
                return ServiceResult.Fail("Failed to send OTP email. Please try again.");
            }

            _logger.LogInformation("OTP sent to {Email}", user.Email);
            return ServiceResult.Ok("OTP sent to your email address.");
        }

        // ── Verify OTP ───────────────────────────────────────
        public async Task<ServiceResult> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null)
                return ServiceResult.Fail("Invalid or expired OTP.");

            var otpRecord = await _db.PasswordResetOtps
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(o => o.UserId == user.UserId && !o.IsUsed);

            if (otpRecord is null || otpRecord.CodeHash != dto.Otp || otpRecord.ExpiresAt < DateTime.UtcNow)
                return ServiceResult.Fail("Invalid or expired OTP.");

            return ServiceResult.Ok("OTP verified successfully. You may now reset your password.");
        }

        // ── Reset Password ───────────────────────────────────
        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null)
                return ServiceResult.Fail("Invalid or expired OTP.");

            var otpRecord = await _db.PasswordResetOtps
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(o => o.UserId == user.UserId && !o.IsUsed);

            if (otpRecord is null || otpRecord.CodeHash != dto.Otp || otpRecord.ExpiresAt < DateTime.UtcNow)
                return ServiceResult.Fail("Invalid or expired OTP.");

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            otpRecord.IsUsed = true;
            otpRecord.UsedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Password reset for {Email}", user.Email);
            return ServiceResult.Ok("Password has been reset successfully.");
        }

        // ── Change Password ──────────────────────────────────
        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null)
                return ServiceResult.Fail("User not found.");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                return ServiceResult.Fail("Incorrect current password.");

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return ServiceResult.Ok("Password changed successfully.");
        }

        // ── Get Me ───────────────────────────────────────────
        public async Task<ServiceResult<object>> GetMeAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null)
                return ServiceResult<object>.Fail("User not found.");

            var data = new
            {
                Id = user.UserId,
                user.Email,
                user.UserName,
                user.Phone,
                user.LocationId,
                user.UserType,
                user.CreatedAt,
                Roles = new List<string> { user.UserType }
            };

            return ServiceResult<object>.Ok(data);
        }

        // ── Private helpers ──────────────────────────────────
        private async Task<string> CreateRefreshTokenAsync(Guid userId)
        {
            var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var rt = new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = userId,
                DeviceId = null, // Set later when device is registered
                TokenHash = tokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(90),
                CreatedAt = DateTime.UtcNow
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();
            return tokenValue;
        }

        private static AuthResponseDto BuildAuthResponse(User user, string jwt, string? refreshToken)
        {
            return new AuthResponseDto
            {
                Token = jwt,
                RefreshToken = refreshToken,
                Expiry = DateTime.UtcNow.AddDays(30),
                UserId = user.UserId.ToString(),
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                LocationId = user.LocationId,
                Roles = new List<string> { user.UserType }
            };
        }
    }
}
