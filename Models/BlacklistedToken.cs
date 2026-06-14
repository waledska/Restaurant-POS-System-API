using System.ComponentModel.DataAnnotations;

namespace WebApisApp.Models
{
    public class BlacklistedToken
    {
        [Key]
        public Guid BlacklistedTokenId { get; set; }

        [Required]
        [StringLength(100)]
        public string Jti { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
