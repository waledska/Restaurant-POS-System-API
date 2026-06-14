namespace WebApisApp.Models
{
    public class SystemPaymentMethod : BaseEntity
    {
        public Guid SystemPaymentMethodId { get; set; }
        public string MethodType { get; set; } = string.Empty; // InstaPay / VodafoneCash / BankAccount / Other / Cash
        public string? AccountData { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    }
}
