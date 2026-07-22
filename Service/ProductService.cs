using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public ProductService(IProductRepository repo, IMapper mapper, AppDbContext context)
        {
            _repo = repo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ProductViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ProductViewDto>(data);
        }

        public async Task<List<ProductViewDto>> GetByTypeAsync(string type)
        {
            var data = await _repo.GetByTypeAsync(type);
            return _mapper.Map<List<ProductViewDto>>(data);
        }

        public async Task<ProductViewDto?> GetByIdAndTypeAsync(long id, string type)
        {
            var data = await _repo.GetByIdAndTypeAsync(id, type);
            if (data == null) return null;
            return _mapper.Map<ProductViewDto>(data);
        }

        public async Task<ProductViewDto> CreateWithTypeAsync(ProductCreateDto dto, string type)
        {
            if (await _repo.ExistsByNameAsync(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductName);
            }

            if (!string.IsNullOrWhiteSpace(dto.SKUCode) && await _repo.ExistsBySKUAsync(dto.SKUCode))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductSKU);
            }

            // Validate unit conversions if provided
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await ValidateUnitConversionsAsync(type, dto.UnitConversions);
            }

            // For manufactured and processed products, recipe is mandatory
            var lowerType = type.ToLower();
            if ((lowerType == "manufactured" || lowerType == "processed") && (dto.Recipe == null || dto.Recipe.Details == null || dto.Recipe.Details.Count == 0))
            {
                throw new MenuGoException(ErrorCodes.RecipeRequired);
            }

            // Validate recipe if provided
            if (dto.Recipe != null)
            {
                await ValidateRecipeAsync(type, dto.Recipe);
            }

            var entity = _mapper.Map<Product>(dto);
            entity.Type = type.ToLower(); // Normalize type casing
            
            if (entity.Type == "tool")
            {
                entity.IsActive = false;
                entity.IsSecret = false;
                entity.IsBatchManage = false;
            }

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync(); // Generates Product ID

            // Save unit conversions
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await SaveUnitConversionsAsync(entity.Id, dto.UnitConversions);
            }

            // Save recipe
            if (dto.Recipe != null)
            {
                await SaveRecipeAsync(entity.Id, dto.Recipe);
            }

            // Populate BInventories for child branches under the same Chain
            var existingBranches = await _context.Branches
                .Where(b => b.ChainId == entity.ChainId)
                .ToListAsync();

            if (existingBranches.Count > 0)
            {
                var existingBranchIds = await _context.BInventories
                    .Where(bi => bi.ProductId == entity.Id)
                    .Select(bi => bi.BranchId)
                    .ToHashSetAsync();

                var bInventories = existingBranches
                    .Where(b => !existingBranchIds.Contains(b.Id))
                    .Select(b => new BInventory
                    {
                        BranchId = b.Id,
                        ProductId = entity.Id,
                        Type = BInventoryHelper.MapProductTypeToBInventoryType(entity.Type),
                        Avg = 0,
                        LeftOver = 0,
                        Quantity = 0,
                        ChainActive = false,
                        BranchActive = false,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (bInventories.Count > 0)
                {
                    await _context.BInventories.AddRangeAsync(bInventories);
                    await _context.SaveChangesAsync();
                }
            }

            return _mapper.Map<ProductViewDto>(entity);
        }

        public async Task<bool> UpdateWithTypeAsync(ProductUpdateDto dto, string type)
        {
            var entity = await _repo.GetByIdAndTypeAsync(dto.Id, type);
            if (entity == null) return false;

            if (await _repo.ExistsByNameExcludeIdAsync(dto.Name, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductName);
            }

            if (!string.IsNullOrWhiteSpace(dto.SKUCode) && await _repo.ExistsBySKUExcludeIdAsync(dto.SKUCode, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductSKU);
            }

            // Validate unit conversions if provided
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await ValidateUnitConversionsAsync(type, dto.UnitConversions);
            }

            // For manufactured and processed products, recipe is mandatory
            var lowerType = type.ToLower();
            if ((lowerType == "manufactured" || lowerType == "processed") && (dto.Recipe == null || dto.Recipe.Details == null || dto.Recipe.Details.Count == 0))
            {
                throw new MenuGoException(ErrorCodes.RecipeRequired);
            }

            // Validate recipe if provided
            if (dto.Recipe != null)
            {
                await ValidateRecipeAsync(type, dto.Recipe, dto.Id);
            }

            _mapper.Map(dto, entity);
            entity.Type = type.ToLower(); // Ensure type remains correct

            if (entity.Type == "tool")
            {
                entity.IsActive = false;
                entity.IsSecret = false;
                entity.IsBatchManage = false;
            }

            await _repo.UpdateAsync(entity);

            // Sync unit conversions if unit conversions array is provided in request
            if (dto.UnitConversions != null)
            {
                await _repo.ClearUnitConversionsAsync(entity.Id);
                if (dto.UnitConversions.Count > 0)
                {
                    await SaveUnitConversionsAsync(entity.Id, dto.UnitConversions);
                }
            }

            // Save/Sync recipe if provided in request
            if (dto.Recipe != null)
            {
                await SaveRecipeAsync(entity.Id, dto.Recipe);
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWithTypeAsync(long id, string type)
        {
            var entity = await _repo.GetByIdAndTypeAsync(id, type);
            if (entity == null) return false;

            await _repo.ClearUnitConversionsAsync(entity.Id);
            await _repo.DeleteAsync(entity);
            await _repo.SaveChangesAsync();
            return true;
        }

        private async Task ValidateUnitConversionsAsync(string type, List<ProductUnitConversionDto> conversions)
        {
            type = type.ToLower();
            var baseCount = conversions.Count(c => c.IsBase);
            if (baseCount == 0)
            {
                throw new MenuGoException(ErrorCodes.BaseUnitRequired);
            }
            if (baseCount > 1)
            {
                throw new MenuGoException(ErrorCodes.OnlyOneBaseUnitAllowed);
            }

            if (type == "processed")
            {
                if (conversions.Count > 1)
                {
                    throw new MenuGoException(ErrorCodes.ProcessedUnitConversionsNotAllowed);
                }
            }

            var unitIds = conversions.Select(c => c.UnitId).ToList();
            if (unitIds.Distinct().Count() != unitIds.Count)
            {
                throw new MenuGoException(ErrorCodes.DuplicateUnitConversion);
            }

            foreach (var conversion in conversions)
            {
                if (!await _repo.UnitExistsAsync(conversion.UnitId))
                {
                    throw new MenuGoException(ErrorCodes.UnitNotFound);
                }

                if (!conversion.IsBase)
                {
                    if (conversion.ConversionPoint % 1 != 0 || conversion.ConversionPoint <= 1 || conversion.ConversionPoint > 1000000000m)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidConversionPoint);
                    }
                }
            }
        }

        private async Task SaveUnitConversionsAsync(long productId, List<ProductUnitConversionDto> conversions)
        {
            var baseDto = conversions.First(c => c.IsBase);
            var baseEntity = new UnitConversion
            {
                ProductId = productId,
                UnitId = baseDto.UnitId,
                BaseId = null,
                ConversionPoint = 1.0m
            };

            await _context.UnitConversions.AddAsync(baseEntity);
            await _context.SaveChangesAsync();

            var derivedDtos = conversions.Where(c => !c.IsBase).ToList();
            if (derivedDtos.Count > 0)
            {
                var derivedEntities = derivedDtos.Select(dto => new UnitConversion
                {
                    ProductId = productId,
                    UnitId = dto.UnitId,
                    BaseId = baseEntity.Id,
                    ConversionPoint = dto.ConversionPoint
                }).ToList();

                await _context.UnitConversions.AddRangeAsync(derivedEntities);
                await _context.SaveChangesAsync();
            }
        }

        private async Task ValidateRecipeAsync(string targetType, ProductRecipeDto recipeDto, long? productId = null)
        {
            targetType = targetType.ToLower();
            if (targetType != "processed" && targetType != "manufactured")
            {
                throw new MenuGoException(ErrorCodes.RecipeNotAllowed);
            }

            if (productId.HasValue && productId.Value > 0)
            {
                var proposedIngredientIds = recipeDto.Details?.Select(d => d.ProductId).ToList() ?? new List<long>();
                if (await WouldFormCycleAsync(productId.Value, proposedIngredientIds))
                {
                    throw new MenuGoException(ErrorCodes.RecipeCycleDetected);
                }
            }

            if (recipeDto.Details == null || recipeDto.Details.Count == 0)
            {
                return;
            }

            foreach (var detail in recipeDto.Details)
            {
                var ingredientType = await _repo.GetProductTypeAsync(detail.ProductId);
                if (ingredientType == null)
                {
                    throw new MenuGoException(ErrorCodes.ProductNotFound);
                }

                ingredientType = ingredientType.ToLower();

                if (targetType == "processed")
                {
                    // Can have processed, manufactured, ingredient, regular (everything except tool)
                    if (ingredientType == "tool")
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                    }
                }
                else if (targetType == "manufactured")
                {
                    // Can have manufactured, ingredient, regular (everything except processed, tool)
                    if (ingredientType == "processed" || ingredientType == "tool")
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                    }
                }

                // Validate quantity: quantity must be >= 0 and <= 1,000,000,000, and max 3 decimal places (allowing trailing zeros after 3 decimal places)
                if (detail.Quantity < 0 || detail.Quantity > 1000000000m || Math.Truncate(detail.Quantity * 1000m) / 1000m != detail.Quantity)
                {
                    throw new MenuGoException(ErrorCodes.InvalidRecipeQuantity);
                }
            }
        }

        private async Task SaveRecipeAsync(long productId, ProductRecipeDto recipeDto)
        {
            if (recipeDto.IsActive)
            {
                // Deactivate all other recipes for this product
                var existingRecipes = await _context.Recipes
                    .Where(r => r.ProductId == productId)
                    .ToListAsync();
                foreach (var r in existingRecipes)
                {
                    r.IsActive = false;
                }
                _context.Recipes.UpdateRange(existingRecipes);
            }

            var recipeEntity = new Recipe
            {
                ProductId = productId,
                VersionName = recipeDto.VersionName,
                IsActive = recipeDto.IsActive
            };

            await _context.Recipes.AddAsync(recipeEntity);
            await _context.SaveChangesAsync();

            if (recipeDto.Details != null && recipeDto.Details.Count > 0)
            {
                var details = recipeDto.Details.Select(d => new RecipesDetailed
                {
                    RecipeId = recipeEntity.Id,
                    ProductId = d.ProductId,
                    Quantity = Math.Truncate(d.Quantity * 1000m) / 1000m
                }).ToList();

                await _context.RecipesDetaileds.AddRangeAsync(details);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<bool> WouldFormCycleAsync(long rootProductId, List<long> proposedIngredientIds)
        {
            var queue = new Queue<long>();
            var visited = new HashSet<long>();

            foreach (var id in proposedIngredientIds)
            {
                if (id == rootProductId)
                {
                    return true; // Direct self-reference
                }
                queue.Enqueue(id);
                visited.Add(id);
            }

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                var activeRecipe = await _context.Recipes
                    .Where(r => r.ProductId == currentId && r.IsActive)
                    .FirstOrDefaultAsync();

                if (activeRecipe != null)
                {
                    var ingredients = await _context.RecipesDetaileds
                        .Where(rd => rd.RecipeId == activeRecipe.Id)
                        .Select(rd => rd.ProductId)
                        .ToListAsync();

                    foreach (var ingredientId in ingredients)
                    {
                        if (ingredientId == rootProductId)
                        {
                            return true; // Cycle detected!
                        }

                        if (!visited.Contains(ingredientId))
                        {
                            visited.Add(ingredientId);
                            queue.Enqueue(ingredientId);
                        }
                    }
                }
            }

            return false;
        }
    }
}
