using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class RecipeServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly RecipeService _service;

        public RecipeServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _service = new RecipeService(_context, _mapper);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Product> SeedProductAsync(string type)
        {
            var product = new Product
            {
                Name = $"Test Product {Guid.NewGuid()}",
                Type = type,
                GroupId = 1,
                ChainId = 1
            };
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowRecipeNotAllowed_WhenProductIsRegular()
        {
            // Arrange
            var product = await SeedProductAsync("regular");
            var dto = new ProductRecipeDto { VersionName = "v1" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(product.Id, dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task CreateAsync_ShouldDeactivateOtherRecipes_WhenProcessedRecipeCreatedActive()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe1);
            await _context.SaveChangesAsync();

            var dto = new ProductRecipeDto { VersionName = "v2", IsActive = true };

            // Act
            var result = await _service.CreateAsync(product.Id, dto);

            // Assert
            Assert.True(result.IsActive);
            var reloadedRecipe1 = await _context.Recipes.FindAsync(recipe1.Id);
            Assert.False(reloadedRecipe1!.IsActive);
        }

        [Fact]
        public async Task CreateAsync_ShouldForceIsActiveTrue_WhenManufacturedFirstRecipeCreatedInactive()
        {
            // Arrange
            var product = await SeedProductAsync("manufactured");
            var dto = new ProductRecipeDto { VersionName = "v1", IsActive = false };

            // Act
            var result = await _service.CreateAsync(product.Id, dto);

            // Assert
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateAsync_ShouldForceIsActiveTrue_WhenProcessedFirstRecipeCreatedInactive()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var dto = new ProductRecipeDto { VersionName = "v1", IsActive = false };

            // Act
            var result = await _service.CreateAsync(product.Id, dto);

            // Assert
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSucceed_WhenRecipeHasDetailsButNoOrderDetails()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var ingredient = await SeedProductAsync("ingredient");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            var detail = new RecipesDetailed { RecipeId = recipe.Id, ProductId = ingredient.Id, Quantity = 1 };
            await _context.RecipesDetaileds.AddAsync(detail);
            await _context.SaveChangesAsync();

            var dto = new ProductRecipeDto 
            { 
                VersionName = "v2", 
                IsActive = true,
                Details = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = ingredient.Id, Quantity = 2.5m }
                }
            };

            // Act
            var result = await _service.UpdateAsync(product.Id, recipe.Id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("v2", result.VersionName);
            
            // Old detail should be cleared and new detail inserted
            var currentDetails = await _context.RecipesDetaileds.Where(rd => rd.RecipeId == recipe.Id).ToListAsync();
            Assert.Single(currentDetails);
            Assert.Equal(ingredient.Id, currentDetails[0].ProductId);
            Assert.Equal(2.5m, currentDetails[0].Quantity);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowRecipeHasDetails_WhenRecipeHasOrderDetails()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            var orderDetail = new OrderDetail { RecipeId = recipe.Id, ProductId = product.Id, Quantity = 1, OrderId = 1, Price = 100 };
            await _context.OrderDetails.AddAsync(orderDetail);
            await _context.SaveChangesAsync();

            var dto = new ProductRecipeDto { VersionName = "v2" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(product.Id, recipe.Id, dto));
            Assert.Equal(ErrorCodes.RecipeHasDetails.Code, ex.Code);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowActiveRecipeRequired_WhenManufacturedDeactivatesOnlyActiveRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("manufactured");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            var dto = new ProductRecipeDto { VersionName = "v1", IsActive = false };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(product.Id, recipe.Id, dto));
            Assert.Equal(ErrorCodes.ActiveRecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowActiveRecipeRequired_WhenProcessedDeactivatesOnlyActiveRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            var dto = new ProductRecipeDto { VersionName = "v1", IsActive = false };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(product.Id, recipe.Id, dto));
            Assert.Equal(ErrorCodes.ActiveRecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task DeleteAsync_ShouldSucceed_WhenAtLeastTwoRecipesExistAndNeitherHasOrderDetails()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var ingredient = await SeedProductAsync("ingredient");
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            var recipe2 = new Recipe { ProductId = product.Id, VersionName = "v2", IsActive = false };
            await _context.Recipes.AddRangeAsync(recipe1, recipe2);
            await _context.SaveChangesAsync();

            var detail = new RecipesDetailed { RecipeId = recipe2.Id, ProductId = ingredient.Id, Quantity = 1 };
            await _context.RecipesDetaileds.AddAsync(detail);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(product.Id, recipe2.Id);

            // Assert
            Assert.True(result);
            var deletedRecipe = await _context.Recipes.FindAsync(recipe2.Id);
            Assert.Null(deletedRecipe);
            
            // Details should be cleaned up too
            var remainingDetails = await _context.RecipesDetaileds.Where(rd => rd.RecipeId == recipe2.Id).ToListAsync();
            Assert.Empty(remainingDetails);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowRecipeHasDetails_WhenRecipeHasOrderDetails()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            var recipe2 = new Recipe { ProductId = product.Id, VersionName = "v2", IsActive = false };
            await _context.Recipes.AddRangeAsync(recipe1, recipe2);
            await _context.SaveChangesAsync();

            var orderDetail = new OrderDetail { RecipeId = recipe2.Id, ProductId = product.Id, Quantity = 1, OrderId = 1, Price = 100 };
            await _context.OrderDetails.AddAsync(orderDetail);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(product.Id, recipe2.Id));
            Assert.Equal(ErrorCodes.RecipeHasDetails.Code, ex.Code);
        }

        [Fact]
        public async Task DeleteAsync_ShouldSucceed_WhenExactlyTwoRecipesAndTheOtherHasOrderDetails()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            var recipe2 = new Recipe { ProductId = product.Id, VersionName = "v2", IsActive = false };
            await _context.Recipes.AddRangeAsync(recipe1, recipe2);
            await _context.SaveChangesAsync();

            // recipe1 (which we are not deleting directly) has order history
            var orderDetail = new OrderDetail { RecipeId = recipe1.Id, ProductId = product.Id, Quantity = 1, OrderId = 1, Price = 100 };
            await _context.OrderDetails.AddAsync(orderDetail);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(product.Id, recipe2.Id);

            // Assert: Should succeed because the deleted recipe itself is clean
            Assert.True(result);
            var deletedRecipe = await _context.Recipes.FindAsync(recipe2.Id);
            Assert.Null(deletedRecipe);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowCannotDeleteOnlyRecipe_WhenManufacturedHasOnlyOneRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("manufactured");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(product.Id, recipe.Id));
            Assert.Equal(ErrorCodes.CannotDeleteOnlyRecipe.Code, ex.Code);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowCannotDeleteOnlyRecipe_WhenProcessedHasOnlyOneRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            var recipe = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(product.Id, recipe.Id));
            Assert.Equal(ErrorCodes.CannotDeleteOnlyRecipe.Code, ex.Code);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowActiveRecipeRequired_WhenManufacturedDeletesOnlyActiveRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("manufactured");
            
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            var recipe2 = new Recipe { ProductId = product.Id, VersionName = "v2", IsActive = false };
            await _context.Recipes.AddRangeAsync(recipe1, recipe2);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(product.Id, recipe1.Id));
            Assert.Equal(ErrorCodes.ActiveRecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowActiveRecipeRequired_WhenProcessedDeletesOnlyActiveRecipe()
        {
            // Arrange
            var product = await SeedProductAsync("processed");
            
            var recipe1 = new Recipe { ProductId = product.Id, VersionName = "v1", IsActive = true };
            var recipe2 = new Recipe { ProductId = product.Id, VersionName = "v2", IsActive = false };
            await _context.Recipes.AddRangeAsync(recipe1, recipe2);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(product.Id, recipe1.Id));
            Assert.Equal(ErrorCodes.ActiveRecipeRequired.Code, ex.Code);
        }
    }
}
