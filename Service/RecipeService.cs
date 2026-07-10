using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class RecipeService : IRecipeService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public RecipeService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductRecipeViewDto>> GetAllByProductAsync(long productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound);
            }

            var recipes = await _context.Recipes
                .Include(r => r.RecipesDetaileds)
                .ThenInclude(rd => rd.Product)
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductRecipeViewDto>>(recipes);
        }

        public async Task<ProductRecipeViewDto> CreateAsync(long productId, ProductRecipeDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound);
            }

            var type = product.Type.ToLower();
            if (type != "processed" && type != "manufactured")
            {
                throw new MenuGoException(ErrorCodes.RecipeNotAllowed);
            }

            // Check cycle detection
            var proposedIngredientIds = dto.Details?.Select(d => d.ProductId).ToList() ?? new List<long>();
            if (await WouldFormCycleAsync(productId, proposedIngredientIds))
            {
                throw new MenuGoException(ErrorCodes.RecipeCycleDetected);
            }

            // Validate details and quantities
            if (dto.Details != null && dto.Details.Count > 0)
            {
                foreach (var detail in dto.Details)
                {
                    var ingredientProduct = await _context.Products.FindAsync(detail.ProductId);
                    if (ingredientProduct == null)
                    {
                        throw new MenuGoException(ErrorCodes.ProductNotFound);
                    }

                    var ingredientType = ingredientProduct.Type.ToLower();

                    if (type == "processed")
                    {
                        if (ingredientType == "tool")
                        {
                            throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                        }
                    }
                    else if (type == "manufactured")
                    {
                        if (ingredientType == "processed" || ingredientType == "tool")
                        {
                            throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                        }
                    }

                    if (detail.Quantity < 0 || detail.Quantity > 1000000000m || Math.Truncate(detail.Quantity * 1000m) / 1000m != detail.Quantity)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeQuantity);
                    }
                }
            }

            // Handle active state
            if (type == "processed" || type == "manufactured")
            {
                var hasAnyActive = await _context.Recipes.AnyAsync(r => r.ProductId == productId && r.IsActive);
                if (!hasAnyActive)
                {
                    // At least 1 active recipe must exist. If none exist, force this to be active.
                    dto.IsActive = true;
                }
                
                if (dto.IsActive)
                {
                    await DeactivateAllRecipesAsync(productId);
                }
            }

            var recipeEntity = new Recipe
            {
                ProductId = productId,
                VersionName = dto.VersionName,
                IsActive = dto.IsActive
            };

            await _context.Recipes.AddAsync(recipeEntity);
            await _context.SaveChangesAsync();

            if (dto.Details != null && dto.Details.Count > 0)
            {
                var details = dto.Details.Select(d => new RecipesDetailed
                {
                    RecipeId = recipeEntity.Id,
                    ProductId = d.ProductId,
                    Quantity = Math.Truncate(d.Quantity * 1000m) / 1000m
                }).ToList();

                await _context.RecipesDetaileds.AddRangeAsync(details);
                await _context.SaveChangesAsync();
            }

            // Reload to map correctly with includes
            var reloaded = await _context.Recipes
                .Include(r => r.RecipesDetaileds)
                .ThenInclude(rd => rd.Product)
                .FirstAsync(r => r.Id == recipeEntity.Id);

            return _mapper.Map<ProductRecipeViewDto>(reloaded);
        }

        public async Task<ProductRecipeViewDto> UpdateAsync(long productId, long recipeId, ProductRecipeDto dto)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipesDetaileds)
                .Include(r => r.OrderDetails)
                .FirstOrDefaultAsync(r => r.Id == recipeId && r.ProductId == productId);

            if (recipe == null)
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound); // Returning ProductNotFound to align with context
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound);
            }

            var type = product.Type.ToLower();

            // Constraint: Can update if order details have none (recipe detailed update is allowed)
            if (recipe.OrderDetails.Count > 0)
            {
                throw new MenuGoException(ErrorCodes.RecipeHasDetails);
            }

            // Check cycle detection
            var proposedIngredientIds = dto.Details?.Select(d => d.ProductId).ToList() ?? new List<long>();
            if (await WouldFormCycleAsync(productId, proposedIngredientIds))
            {
                throw new MenuGoException(ErrorCodes.RecipeCycleDetected);
            }

            // Validate details and quantities
            if (dto.Details != null && dto.Details.Count > 0)
            {
                foreach (var detail in dto.Details)
                {
                    var ingredientProduct = await _context.Products.FindAsync(detail.ProductId);
                    if (ingredientProduct == null)
                    {
                        throw new MenuGoException(ErrorCodes.ProductNotFound);
                    }

                    var ingredientType = ingredientProduct.Type.ToLower();

                    if (type == "processed")
                    {
                        if (ingredientType == "tool")
                        {
                            throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                        }
                    }
                    else if (type == "manufactured")
                    {
                        if (ingredientType == "processed" || ingredientType == "tool")
                        {
                            throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                        }
                    }

                    if (detail.Quantity < 0 || detail.Quantity > 1000000000m || Math.Truncate(detail.Quantity * 1000m) / 1000m != detail.Quantity)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeQuantity);
                    }
                }
            }

            // Handle active state
            if (type == "processed" || type == "manufactured")
            {
                if (recipe.IsActive && !dto.IsActive)
                {
                    // Trying to deactivate. Ensure there is another active recipe.
                    var otherActiveExists = await _context.Recipes
                        .AnyAsync(r => r.ProductId == productId && r.Id != recipeId && r.IsActive);
                    if (!otherActiveExists)
                    {
                        throw new MenuGoException(ErrorCodes.ActiveRecipeRequired);
                    }
                }

                if (dto.IsActive)
                {
                    await DeactivateAllRecipesAsync(productId);
                }
                recipe.IsActive = dto.IsActive;
            }

            recipe.VersionName = dto.VersionName;
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();

            // Clear old details
            var oldDetails = await _context.RecipesDetaileds
                .Where(rd => rd.RecipeId == recipe.Id)
                .ToListAsync();
            _context.RecipesDetaileds.RemoveRange(oldDetails);
            await _context.SaveChangesAsync();

            // Insert new details
            if (dto.Details != null && dto.Details.Count > 0)
            {
                var details = dto.Details.Select(d => new RecipesDetailed
                {
                    RecipeId = recipe.Id,
                    ProductId = d.ProductId,
                    Quantity = Math.Truncate(d.Quantity * 1000m) / 1000m
                }).ToList();

                await _context.RecipesDetaileds.AddRangeAsync(details);
                await _context.SaveChangesAsync();
            }

            // Reload and return
            var reloaded = await _context.Recipes
                .Include(r => r.RecipesDetaileds)
                .ThenInclude(rd => rd.Product)
                .FirstAsync(r => r.Id == recipe.Id);

            return _mapper.Map<ProductRecipeViewDto>(reloaded);
        }

        public async Task<bool> DeleteAsync(long productId, long recipeId)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipesDetaileds)
                .Include(r => r.OrderDetails)
                .FirstOrDefaultAsync(r => r.Id == recipeId && r.ProductId == productId);

            if (recipe == null)
            {
                return false;
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound);
            }

            var type = product.Type.ToLower();

            // Constraint: Can delete if order details has none
            if (recipe.OrderDetails.Count > 0)
            {
                throw new MenuGoException(ErrorCodes.RecipeHasDetails);
            }

            if (type == "processed" || type == "manufactured")
            {
                // Cannot delete if there is only 1 recipe
                var totalRecipes = await _context.Recipes.CountAsync(r => r.ProductId == productId);
                if (totalRecipes <= 1)
                {
                    throw new MenuGoException(ErrorCodes.CannotDeleteOnlyRecipe);
                }

                // If deleting the active recipe, ensure another is active
                if (recipe.IsActive)
                {
                    var otherActiveExists = await _context.Recipes
                        .AnyAsync(r => r.ProductId == productId && r.Id != recipeId && r.IsActive);
                    if (!otherActiveExists)
                    {
                        throw new MenuGoException(ErrorCodes.ActiveRecipeRequired);
                    }
                }
            }

            // Remove detailed recipe ingredients first to avoid FK restrict constraint violation
            if (recipe.RecipesDetaileds != null && recipe.RecipesDetaileds.Count > 0)
            {
                _context.RecipesDetaileds.RemoveRange(recipe.RecipesDetaileds);
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task DeactivateAllRecipesAsync(long productId)
        {
            var existingRecipes = await _context.Recipes
                .Where(r => r.ProductId == productId && r.IsActive)
                .ToListAsync();

            foreach (var r in existingRecipes)
            {
                r.IsActive = false;
            }
            _context.Recipes.UpdateRange(existingRecipes);
            await _context.SaveChangesAsync();
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
