using Microsoft.EntityFrameworkCore;
using WebApisApp.Data;
using WebApisApp.DTOs.Common;
using WebApisApp.Helpers;
using WebApisApp.Models;

namespace WebApisApp.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;

        public ProductService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<List<ProductDto>>> GetProductsAsync(string? type, bool? isActive = true, int page = 1, int pageSize = 50)
        {
            var query = _db.Products.Where(p => !p.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(p => p.ProductType == type);
                
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                ProductType = p.ProductType,
                BaseUnit = p.BaseUnitName,
                GlobalAverageCost = p.GlobalAverageCost,
                SellingPrice = p.SellingPrice,
                IsActive = p.IsActive,
                HasInitialBalance = p.StockMovements.Any(m => m.MovementType == "InitialBalance"),
                CreatedAt = p.CreatedAt
            }).ToListAsync();

            return ServiceResult<List<ProductDto>>.Ok(products);
        }

        public async Task<ServiceResult<ProductDto>> GetProductByIdAsync(Guid id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);
            if (p == null) return ServiceResult<ProductDto>.Fail("Product not found.");

            return ServiceResult<ProductDto>.Ok(new ProductDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                ProductType = p.ProductType,
                BaseUnit = p.BaseUnitName,
                GlobalAverageCost = p.GlobalAverageCost,
                SellingPrice = p.SellingPrice,
                IsActive = p.IsActive,
                HasInitialBalance = await _db.StockMovements.AnyAsync(m => m.ProductId == id && m.MovementType == "InitialBalance"),
                CreatedAt = p.CreatedAt
            });
        }

        public async Task<ServiceResult<ProductDto>> CreateProductAsync(ProductCreateDto dto)
        {
            string generatedCode = $"PRD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var p = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductCode = generatedCode,
                ProductName = dto.ProductName,
                ProductType = dto.ProductType,
                BaseUnitName = dto.BaseUnit,
                SellingPrice = dto.SellingPrice,
                GlobalAverageCost = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(p);
            await _db.SaveChangesAsync();

            return await GetProductByIdAsync(p.ProductId);
        }

        public async Task<ServiceResult<ProductDto>> UpdateProductAsync(Guid id, ProductUpdateDto dto)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);
            if (p == null) return ServiceResult<ProductDto>.Fail("Product not found.");

            // Validation: transition from Manufactured to Raw
            if (p.ProductType == "Manufactured" && dto.ProductType == "Raw")
            {
                var hasRecipeAsParent = await _db.ProductRecipes.AnyAsync(r => r.ManufacturedProductId == id);
                var hasRecipeAsChild = await _db.ProductRecipes.AnyAsync(r => r.RawProductId == id);
                
                if (hasRecipeAsParent || hasRecipeAsChild)
                {
                    return ServiceResult<ProductDto>.Fail("Cannot change to Raw: This product is currently linked to one or more recipes. Remove all recipe associations first.");
                }
            }

            p.ProductName = dto.ProductName;
            p.ProductType = dto.ProductType;
            p.BaseUnitName = dto.BaseUnit;
            p.SellingPrice = dto.SellingPrice;
            p.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();
            return await GetProductByIdAsync(p.ProductId);
        }

        public async Task<ServiceResult> DeleteProductAsync(Guid id)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);
            if (p == null) return ServiceResult.Fail("Product not found.");

            // Safety check: existing stock
            var hasStock = await _db.StockBalances.AnyAsync(b => b.ProductId == id && b.QuantityOnHand > 0);
            if (hasStock)
                return ServiceResult.Fail("Cannot delete product: There is existing stock in one or more locations. Clear the stock first.");

            // Safety check: is it a component in any recipe?
            var usedInRecipe = await _db.ProductRecipes.AnyAsync(r => r.RawProductId == id || r.ManufacturedProductId == id);
            if (usedInRecipe)
                return ServiceResult.Fail("Cannot delete product: It is still linked to a recipe. Remove it from the recipe first.");

            p.IsDeleted = true;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Product deleted successfully (Soft Delete).");
        }

        public async Task<ServiceResult<List<ProductRecipeDto>>> GetRecipesForProductAsync(Guid manufacturedProductId)
        {
            var recipes = await _db.ProductRecipes
                .Where(r => r.ManufacturedProductId == manufacturedProductId && !r.IsDeleted)
                .Select(r => new ProductRecipeDto
                {
                    RecipeId = r.RecipeId,
                    ManufacturedProductId = r.ManufacturedProductId,
                    RawProductId = r.RawProductId,
                    QuantityNeeded = r.QuantityNeeded
                }).ToListAsync();

            return ServiceResult<List<ProductRecipeDto>>.Ok(recipes);
        }

        public async Task<ServiceResult> AddOrUpdateRecipeAsync(Guid manufacturedProductId, List<ProductRecipeCreateDto> recipeItems)
        {
            if (recipeItems == null || !recipeItems.Any())
                return ServiceResult.Fail("No recipe items provided.");

            // 1. Basic Validation
            var mProduct = await _db.Products.FindAsync(manufacturedProductId);
            if (mProduct == null || mProduct.ProductType != "Manufactured" || mProduct.IsDeleted)
                return ServiceResult.Fail("Invalid or non-existent manufactured product.");

            // 2. Pre-process and Group Input (Additive grouping for duplicates in same request)
            var consolidatedItems = recipeItems
                .GroupBy(i => i.RawProductId)
                .Select(g => new { RawProductId = g.Key, TotalQty = g.Sum(x => x.QuantityNeeded) })
                .ToList();

            // 3. Detailed Validation for Raw Materials
            foreach (var item in consolidatedItems)
            {
                if (item.RawProductId == manufacturedProductId)
                    return ServiceResult.Fail($"Self-reference error: Product cannot be a component of its own recipe.");

                var rawProduct = await _db.Products.FindAsync(item.RawProductId);
                if (rawProduct == null || rawProduct.ProductType != "Raw" || rawProduct.IsDeleted)
                    return ServiceResult.Fail($"Invalid material ID {item.RawProductId}: Product not found or is not a raw material.");

                if (item.TotalQty <= 0)
                    return ServiceResult.Fail("Quantity needed must be greater than zero.");
            }

            // 4. Merge/Additive Logic
            var existingRecipes = await _db.ProductRecipes
                .Where(r => r.ManufacturedProductId == manufacturedProductId && !r.IsDeleted)
                .ToListAsync();

            foreach (var incoming in consolidatedItems)
            {
                var existing = existingRecipes.FirstOrDefault(r => r.RawProductId == incoming.RawProductId);
                if (existing != null)
                {
                    // Update existing: Add amount
                    existing.QuantityNeeded += incoming.TotalQty;
                }
                else
                {
                    // Insert new
                    _db.ProductRecipes.Add(new ProductRecipe
                    {
                        RecipeId = Guid.NewGuid(),
                        ManufacturedProductId = manufacturedProductId,
                        RawProductId = incoming.RawProductId,
                        QuantityNeeded = incoming.TotalQty,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Recipes updated successfully (Additive Merge).");
        }

        public async Task<ServiceResult> RemoveRecipeItemAsync(Guid manufacturedProductId, Guid rawProductId)
        {
            var item = await _db.ProductRecipes.FirstOrDefaultAsync(r => r.ManufacturedProductId == manufacturedProductId && r.RawProductId == rawProductId && !r.IsDeleted);
            if (item == null) return ServiceResult.Fail("Recipe item association not found.");

            _db.ProductRecipes.Remove(item);
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Recipe item removed successfully (Hard Deleted).");
        }
    }
}
