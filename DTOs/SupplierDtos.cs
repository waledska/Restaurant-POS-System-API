namespace WebApisApp.DTOs.Common
{
    public class SupplierDto
    {
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupplierCreateDto
    {
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal OpeningBalance { get; set; } = 0;
    }

    public class SupplierUpdateDto : SupplierCreateDto
    {
        public bool IsActive { get; set; }
    }

}
