using System.ComponentModel.DataAnnotations;

namespace WebApisApp.DTOs.Common
{
    public class ProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal GlobalAverageCost { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; }
        public bool HasInitialBalance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductCreateDto
    {
        public string ProductName { get; set; } = string.Empty;
        
        [Required]
        [RegularExpression("^(Raw|Manufactured)$", ErrorMessage = "Product Type must be exactly 'Raw' or 'Manufactured'.")]
        public string ProductType { get; set; } = string.Empty; // "Raw" or "Manufactured"
        
        public string BaseUnit { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
    }

    public class ProductUpdateDto : ProductCreateDto
    {
        public bool IsActive { get; set; }
    }

    public class ProductRecipeDto
    {
        public Guid RecipeId { get; set; }
        public Guid ManufacturedProductId { get; set; }
        public Guid RawProductId { get; set; }
        public decimal QuantityNeeded { get; set; }
    }

    public class ProductRecipeCreateDto
    {
        public Guid RawProductId { get; set; }
        public decimal QuantityNeeded { get; set; }
    }
}
