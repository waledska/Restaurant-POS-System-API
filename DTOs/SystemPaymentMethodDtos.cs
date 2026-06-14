namespace WebApisApp.DTOs.Common
{
    public class SystemPaymentMethodDto
    {
        public Guid SystemPaymentMethodId { get; set; }
        public string MethodType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class SystemPaymentMethodCreateDto
    {
        public string MethodType { get; set; } = string.Empty; // Cash, Bank, Wallet
        public string? Details { get; set; }
        public string? Notes { get; set; }
    }

    public class SystemPaymentMethodUpdateDto : SystemPaymentMethodCreateDto
    {
        public bool IsActive { get; set; }
    }
}
