using System.ComponentModel.DataAnnotations;

namespace WebApisApp.Models
{
    public class GlobalSettings : BaseEntity
    {
        [Key]
        public Guid SettingId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
