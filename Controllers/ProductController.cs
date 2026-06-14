using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Services;

namespace WebApisApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? type,
            [FromQuery] bool? isActive = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _productService.GetProductsAsync(type, isActive, page, pageSize);
            return Ok(ApiResponse<List<ProductDto>>.Ok(result.Data!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _productService.GetProductByIdAsync(id);
            if (!result.Success) return NotFound(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<ProductDto>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var result = await _productService.CreateProductAsync(dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<ProductDto>.Ok(result.Data!, "Created successfully."));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
        {
            var result = await _productService.UpdateProductAsync(id, dto);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<ProductDto>.Ok(result.Data!, "Updated successfully."));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }


        // --- Recipes ---
        [HttpGet("{id}/recipes")]
        public async Task<IActionResult> GetRecipes(Guid id)
        {
            var result = await _productService.GetRecipesForProductAsync(id);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse<List<ProductRecipeDto>>.Ok(result.Data!));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpPost("{id}/recipes")]
        public async Task<IActionResult> UpdateRecipes(Guid id, [FromBody] List<ProductRecipeCreateDto> items)
        {
            var result = await _productService.AddOrUpdateRecipeAsync(id, items);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }

        [Authorize(Roles = "Admin,WarehouseManager")]
        [HttpDelete("{id}/recipes/{rawProductId}")]
        public async Task<IActionResult> RemoveRecipeItem(Guid id, Guid rawProductId)
        {
            var result = await _productService.RemoveRecipeItemAsync(id, rawProductId);
            if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
            return Ok(ApiResponse.Ok(result.Message));
        }
    }
}
