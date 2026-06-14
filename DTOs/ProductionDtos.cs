namespace WebApisApp.DTOs.Common
{
    public class ProductionOperationDto
    {
        public Guid ProductionOperationId { get; set; }
        public Guid LocationId { get; set; }
        public Guid ManufacturedProductId { get; set; }
        public decimal QuantityProduced { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime ProductionDate { get; set; }
    }

    public class ProductionCreateDto
    {
        public Guid LocationId { get; set; }
        public Guid ManufacturedProductId { get; set; }
        public decimal QuantityProduced { get; set; }
    }
}
