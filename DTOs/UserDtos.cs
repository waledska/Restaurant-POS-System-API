namespace WebApisApp.DTOs.Common
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string UserType { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserCreateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string UserType { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
    }

    public class UserUpdateDto
    {
        public string UserName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string UserType { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UserResetPasswordAdminDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
