using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(ApplicationDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<ServiceResult<List<UserDto>>> GetAllUsersAsync()
        {
            var users = await _db.Users
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    Phone = u.Phone,
                    UserType = u.UserType,
                    LocationId = u.LocationId,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                }).ToListAsync();

            return ServiceResult<List<UserDto>>.Ok(users);
        }

        public async Task<ServiceResult<UserDto>> GetUserByIdAsync(Guid id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return ServiceResult<UserDto>.Fail("User not found.");

            return ServiceResult<UserDto>.Ok(new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.Email,
                Phone = u.Phone,
                UserType = u.UserType,
                LocationId = u.LocationId,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto)
        {
            if (!User.ValidRoles.Contains(dto.UserType))
                return ServiceResult<UserDto>.Fail($"Invalid UserType. Valid roles are: {string.Join(", ", User.ValidRoles)}.");

            if (dto.LocationId.HasValue)
            {
                var locationExists = await _db.Locations.AnyAsync(l => l.LocationId == dto.LocationId.Value);
                if (!locationExists)
                    return ServiceResult<UserDto>.Fail("Invalid LocationId. The specified location does not exist.");
            }

            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email || u.UserName == dto.UserName);
            if (exists) return ServiceResult<UserDto>.Fail("Email or Username already exists.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserName = dto.UserName,
                Email = dto.Email,
                Phone = dto.Phone,
                UserType = dto.UserType,
                LocationId = dto.LocationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return await GetUserByIdAsync(user.UserId);
        }

        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return ServiceResult<UserDto>.Fail("User not found.");

            if (!User.ValidRoles.Contains(dto.UserType))
                return ServiceResult<UserDto>.Fail($"Invalid UserType. Valid roles are: {string.Join(", ", User.ValidRoles)}.");

            if (dto.LocationId.HasValue && dto.LocationId != user.LocationId)
            {
                var locationExists = await _db.Locations.AnyAsync(l => l.LocationId == dto.LocationId.Value);
                if (!locationExists)
                    return ServiceResult<UserDto>.Fail("Invalid LocationId. The specified location does not exist.");
            }

            // Uniqueness check for UserName if changed
            if (user.UserName != dto.UserName)
            {
                var nameExists = await _db.Users.AnyAsync(u => u.UserName == dto.UserName && u.UserId != id);
                if (nameExists) return ServiceResult<UserDto>.Fail("Username already exists.");
                user.UserName = dto.UserName;
            }

            user.Phone = dto.Phone;
            user.UserType = dto.UserType;
            user.LocationId = dto.LocationId;
            user.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return await GetUserByIdAsync(user.UserId);
        }

        public async Task<ServiceResult> ToggleActivationAsync(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return ServiceResult.Fail("User not found.");

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok($"User activation set to {user.IsActive}");
        }

        public async Task<ServiceResult> ResetUserPasswordAsync(Guid userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return ServiceResult.Fail("Password must be at least 4 characters long.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return ServiceResult.Fail("User not found.");

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();

            return ServiceResult.Ok("Password has been reset successfully.");
        }
    }
}
