using System.ComponentModel.DataAnnotations;

namespace WebApisApp.Models
{
    public class Tenant : BaseEntity
    {
        [Key]
        public Guid TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SubscriptionPlan { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? SubscriptionEndDate { get; set; }
    }
}
