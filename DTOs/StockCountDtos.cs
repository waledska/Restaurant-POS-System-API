namespace WebApisApp.DTOs.Common
{
    public class StockCountDto
    {
        public Guid StockCountId { get; set; }
        public Guid LocationId { get; set; }
        public DateTime CountDate { get; set; }
        public string Status { get; set; } = string.Empty; // قيد الانتظار, معتمد
        public string? Notes { get; set; }
    }

    public class StockCountCreateDto
    {
        public Guid LocationId { get; set; }
        public string? Notes { get; set; }
        public List<StockCountItemCreateDto> Items { get; set; } = new List<StockCountItemCreateDto>();
    }

    public class StockCountItemCreateDto
    {
        public Guid ProductId { get; set; }
        public decimal? ActualQuantity { get; set; }
    }

    public class StockCountItemDto
    {
        public Guid StockCountItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal? ActualQty { get; set; }
        public decimal? DifferenceQty { get; set; }
    }

    public class StockCountDetailDto : StockCountDto
    {
        public List<StockCountItemDto> Items { get; set; } = new List<StockCountItemDto>();
    }
}
