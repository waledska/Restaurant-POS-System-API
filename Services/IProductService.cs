using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IProductService
    {
        Task<ServiceResult<List<ProductDto>>> GetProductsAsync(string? type, bool? isActive = true, int page = 1, int pageSize = 50);
        Task<ServiceResult<ProductDto>> GetProductByIdAsync(Guid id);
        Task<ServiceResult<ProductDto>> CreateProductAsync(ProductCreateDto dto);
        Task<ServiceResult<ProductDto>> UpdateProductAsync(Guid id, ProductUpdateDto dto);
        Task<ServiceResult> DeleteProductAsync(Guid id);
        
        Task<ServiceResult<List<ProductRecipeDto>>> GetRecipesForProductAsync(Guid manufacturedProductId);
        Task<ServiceResult> AddOrUpdateRecipeAsync(Guid manufacturedProductId, List<ProductRecipeCreateDto> recipeItems);
        Task<ServiceResult> RemoveRecipeItemAsync(Guid manufacturedProductId, Guid rawProductId);
    }
}
