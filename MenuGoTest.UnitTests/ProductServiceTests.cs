using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class ProductServiceTests : IDisposable
    {
        private readonly Mock<IProductRepository> _repoMock;
        private readonly Mock<IUnitRepository> _unitRepoMock;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _repoMock = new Mock<IProductRepository>();
            _unitRepoMock = new Mock<IUnitRepository>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Add a default group and base material product to satisfy pre-requisites
            _context.Groups.Add(new Group { Id = 1, Name = "Default Group" });
            _context.Products.Add(new Product
            {
                Id = 9999,
                Name = "Base Ingredient",
                Type = ProductType.Ingredient,
                IsSellable = false,
                SellPrice = 0
            });
            _context.SaveChanges();

            _service = new ProductService(_repoMock.Object, _unitRepoMock.Object, _mapper, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeNotAllowed_WhenToolHasRecipe()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Hammer",
                IsSellable = false,
                SellPrice = 0,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 2, Quantity = 1 }
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "tool"));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeNotAllowed_WhenRegularHasRecipe()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Table",
                IsSellable = true,
                SellPrice = 100,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 2, Quantity = 1 }
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "regular"));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeNotAllowed_WhenIngredientHasRecipe()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Salt",
                IsSellable = false,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 2, Quantity = 1 }
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "ingredient"));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeRequired_WhenManufacturedHasNoRecipe()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Car",
                IsSellable = true,
                SellPrice = 500000000,
                Recipe = null // Missing recipe
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "manufactured"));
            Assert.Equal(ErrorCodes.RecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeRequired_WhenManufacturedHasRecipeButNoDetails()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Car",
                IsSellable = true,
                SellPrice = 500000000,
                Recipe = new List<ProductRecipeDetailedDto>() // Empty details
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "manufactured"));
            Assert.Equal(ErrorCodes.RecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowRecipeRequired_WhenProcessedHasNoRecipe()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Coffee",
                IsSellable = true,
                SellPrice = 30000,
                Recipe = null
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.RecipeRequired.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowInvalidRecipeIngredient_WhenProcessedRecipeContainsTool()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Soup",
                IsSellable = true,
                SellPrice = 50000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 10, Quantity = 1 } // tool
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(10)).ReturnsAsync(ProductType.Tool);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.InvalidRecipeIngredient.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowInvalidRecipeIngredient_WhenManufacturedRecipeContainsProcessed()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Cake Mix",
                IsSellable = true,
                SellPrice = 80000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 20, Quantity = 1 } // processed
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(20)).ReturnsAsync(ProductType.Processed);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "manufactured"));
            Assert.Equal(ErrorCodes.InvalidRecipeIngredient.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowSellPriceRequired_WhenRegularHasZeroPrice()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Free Gift",
                IsSellable = true,
                SellPrice = 0
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "regular"));
            Assert.Equal(ErrorCodes.SellPriceRequired.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldAllowValidTool_WithZeroSellPriceAndNotSellable()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Screwdriver",
                IsSellable = false,
                SellPrice = 0
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateWithTypeAsync(dto, "tool");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SellPrice);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Product>(p => p.SellPrice == 0 && p.IsSellable == false)), Times.Once);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldValidateRecipeQuantity_AndTruncateTrailingZeros()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Fried Rice Special",
                IsSellable = true,
                SellPrice = 60000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 10, Quantity = 1.123000m }, // valid, should be cut to 1.123
                    new ProductRecipeDetailedDto { ProductId = 11, Quantity = 2.0m } // valid
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeAsync(10)).ReturnsAsync("ingredient");
            _repoMock.Setup(r => r.GetProductTypeAsync(11)).ReturnsAsync("ingredient");
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(10)).ReturnsAsync(ProductType.Ingredient);
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(11)).ReturnsAsync(ProductType.Ingredient);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateWithTypeAsync(dto, "processed");

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);

            // Fetch created recipe details from in-memory context to assert truncation
            var savedDetails = await _context.RecipesDetaileds.ToListAsync();
            Assert.Equal(2, savedDetails.Count);
            
            var detailsList = savedDetails.FindAll(d => d.IngredientProductId == 10);
            Assert.Single(detailsList);
            Assert.Equal(1.123m, detailsList[0].Quantity);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowInvalidRecipeQuantity_WhenMoreThan3DecimalPlacesAndFourthIsNonZero()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Fried Rice Failed",
                IsSellable = true,
                SellPrice = 60000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 10, Quantity = 1.1234m } // invalid
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeAsync(10)).ReturnsAsync("ingredient");
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(10)).ReturnsAsync(ProductType.Ingredient);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.InvalidRecipeQuantity.Code, ex.Code);
        }

        [Fact]
        public async Task UpdateWithTypeAsync_ShouldThrowRecipeNotAllowed_WhenToolUpdatedWithRecipe()
        {
            // Arrange
            var dto = new ProductUpdateDto
            {
                Id = 1,
                GroupId = 1,
                Name = "Hammer",
                IsSellable = false,
                SellPrice = 0,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 2, Quantity = 1 }
                }
            };

            var existing = new Product { Id = 1, Name = "Hammer", Type = ProductType.Tool };

            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, ProductType.Tool)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, "tool")).ReturnsAsync(existing);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(dto.Name, dto.Id)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateWithTypeAsync(dto, "tool"));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task UpdateWithTypeAsync_ShouldThrowRecipeCycleDetected_WhenDirectSelfReference()
        {
            // Arrange
            var dto = new ProductUpdateDto
            {
                Id = 1,
                GroupId = 1,
                Name = "Pizza",
                IsSellable = true,
                SellPrice = 10000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 1, Quantity = 1 } // self reference (Pizza -> Pizza)
                }
            };

            var existing = new Product { Id = 1, Name = "Pizza", Type = ProductType.Processed };

            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, ProductType.Processed)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, "processed")).ReturnsAsync(existing);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(dto.Name, dto.Id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(1)).ReturnsAsync(ProductType.Processed);
            _repoMock.Setup(r => r.GetProductTypeAsync(1)).ReturnsAsync("processed");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.RecipeCycleDetected.Code, ex.Code);
        }

        [Fact]
        public async Task UpdateWithTypeAsync_ShouldThrowRecipeCycleDetected_WhenCircularReferenceOfLength2()
        {
            // Arrange
            // Product 1 (Pizza) is updated to depend on Product 2 (Dough)
            // Product 2's active recipe already depends on Product 1 (Pizza)
            var dto = new ProductUpdateDto
            {
                Id = 1,
                GroupId = 1,
                Name = "Pizza",
                IsSellable = true,
                SellPrice = 10000,
                Recipe = new List<ProductRecipeDetailedDto>
                {
                    new ProductRecipeDetailedDto { ProductId = 2, Quantity = 1 }
                }
            };

            var existing = new Product { Id = 1, Name = "Pizza", Type = ProductType.Processed };

            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, ProductType.Processed)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.GetByIdAndTypeAsync(dto.Id, "processed")).ReturnsAsync(existing);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(dto.Name, dto.Id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetProductTypeEnumAsync(2)).ReturnsAsync(ProductType.Manufactured);
            _repoMock.Setup(r => r.GetProductTypeAsync(2)).ReturnsAsync("manufactured");

            // Setup DB: Product 2 has active recipe depending on Product 1
            var recipeDetail = new RecipesDetailed { ParentProductId = 2, IngredientProductId = 1, Quantity = 1 };

            await _context.RecipesDetaileds.AddAsync(recipeDetail);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.RecipeCycleDetected.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowOnlyOneBaseUnitAllowed_WhenMultipleBaseUnitsSupplied()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Too Many Bases",
                IsSellable = true,
                SellPrice = 10000,
                UnitConversions = new List<ProductUnitConversionDto>
                {
                    new ProductUnitConversionDto { UnitId = 2, ConversionPoint = 1, IsBase = true },
                    new ProductUnitConversionDto { UnitId = 3, ConversionPoint = 1, IsBase = true } // second base
                }
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "regular"));
            Assert.Equal(ErrorCodes.OnlyOneBaseUnitAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldThrowProcessedUnitConversionsNotAllowed_WhenProcessedProductHasMultipleUnitConversions()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                GroupId = 1,
                Name = "Processed Multi Units",
                IsSellable = true,
                SellPrice = 10000,
                UnitConversions = new List<ProductUnitConversionDto>
                {
                    new ProductUnitConversionDto { UnitId = 2, ConversionPoint = 1, IsBase = true },
                    new ProductUnitConversionDto { UnitId = 3, ConversionPoint = 20, IsBase = false } // derived conversion, not allowed for processed
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateWithTypeAsync(dto, "processed"));
            Assert.Equal(ErrorCodes.ProcessedUnitConversionsNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task CreateWithTypeAsync_ShouldCreateBInventoryForChildBranches()
        {
            // Arrange
            long chainId = 10;
            var branch1 = new Branch { Id = 1, ChainId = chainId, Name = "Branch 1" };
            var branch2 = new Branch { Id = 2, ChainId = chainId, Name = "Branch 2" };
            await _context.Branches.AddRangeAsync(branch1, branch2);
            await _context.SaveChangesAsync();

            var dto = new ProductCreateDto
            {
                GroupId = 1,
                ChainId = chainId,
                Name = "New Milk Tea",
                SKUCode = "TEA-001",
                IsSellable = true,
                SellPrice = 35000
            };

            _repoMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsBySKUAsync(dto.SKUCode)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>()))
                .Callback<Product>(p => p.Id = 100)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateWithTypeAsync(dto, "regular");

            // Assert
            Assert.NotNull(result);
            var bInventories = await _context.BInventories.Where(bi => bi.ProductId == 100).ToListAsync();
            Assert.Equal(2, bInventories.Count);
            Assert.All(bInventories, bi =>
            {
                Assert.False(bi.ChainActive);
                Assert.False(bi.BranchActive);
                Assert.Equal(0, bi.Quantity);
                Assert.Equal(0, bi.LeftOver);
                Assert.Equal(0, bi.Avg);
            });
        }
    }
}
