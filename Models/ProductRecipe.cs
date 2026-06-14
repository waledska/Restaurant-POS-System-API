namespace WebApisApp.Models
{
    public class ProductRecipe : BaseEntity
    {
        public Guid RecipeId { get; set; }
        public Guid ManufacturedProductId { get; set; }
        public Guid RawProductId { get; set; }
        public decimal QuantityNeeded { get; set; }

        // Navigation
        public Product ManufacturedProduct { get; set; } = null!;
        public Product RawProduct { get; set; } = null!;
    }
}
