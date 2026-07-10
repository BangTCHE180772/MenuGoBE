using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class ProductControllersTests
    {
        private readonly Mock<IProductService> _serviceMock;

        public ProductControllersTests()
        {
            _serviceMock = new Mock<IProductService>();
        }

        // --- 1. ManufacturedProductController Tests ---

        [Fact]
        public async Task ManufacturedController_GetAll_ShouldReturnOk()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            var expectedList = new List<ProductViewDto> { new ProductViewDto { Id = 1, Name = "M1" } };
            _serviceMock.Setup(s => s.GetByTypeAsync("manufactured")).ReturnsAsync(expectedList);

            // Act
            var result = await controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedList, okResult.Value);
        }

        [Fact]
        public async Task ManufacturedController_GetById_ShouldReturnOk_WhenExists()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            var expectedProduct = new ProductViewDto { Id = 1, Name = "M1" };
            _serviceMock.Setup(s => s.GetByIdAndTypeAsync(1, "manufactured")).ReturnsAsync(expectedProduct);

            // Act
            var result = await controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedProduct, okResult.Value);
        }

        [Fact]
        public async Task ManufacturedController_GetById_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            _serviceMock.Setup(s => s.GetByIdAndTypeAsync(1, "manufactured")).ReturnsAsync((ProductViewDto)null);

            // Act
            var result = await controller.GetById(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ManufacturedController_Create_ShouldReturnOk()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            var dto = new ProductCreateDto { Name = "New M" };
            var expectedProduct = new ProductViewDto { Id = 1, Name = "New M" };
            _serviceMock.Setup(s => s.CreateWithTypeAsync(dto, "manufactured")).ReturnsAsync(expectedProduct);

            // Act
            var result = await controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedProduct, okResult.Value);
        }

        [Fact]
        public async Task ManufacturedController_Update_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            var dto = new ProductUpdateDto { Id = 1, Name = "Update M" };
            _serviceMock.Setup(s => s.UpdateWithTypeAsync(dto, "manufactured")).ReturnsAsync(true);

            // Act
            var result = await controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ManufacturedController_Update_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            var dto = new ProductUpdateDto { Id = 1, Name = "Update M" };
            _serviceMock.Setup(s => s.UpdateWithTypeAsync(dto, "manufactured")).ReturnsAsync(false);

            // Act
            var result = await controller.Update(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ManufacturedController_Delete_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            _serviceMock.Setup(s => s.DeleteWithTypeAsync(1, "manufactured")).ReturnsAsync(true);

            // Act
            var result = await controller.Delete(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ManufacturedController_Delete_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            var controller = new ManufacturedProductController(_serviceMock.Object);
            _serviceMock.Setup(s => s.DeleteWithTypeAsync(1, "manufactured")).ReturnsAsync(false);

            // Act
            var result = await controller.Delete(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // --- 2. ProcessedProductController Tests ---

        [Fact]
        public async Task ProcessedController_GetAll_ShouldReturnOk()
        {
            // Arrange
            var controller = new ProcessedProductController(_serviceMock.Object);
            var expectedList = new List<ProductViewDto> { new ProductViewDto { Id = 1, Name = "P1" } };
            _serviceMock.Setup(s => s.GetByTypeAsync("processed")).ReturnsAsync(expectedList);

            // Act
            var result = await controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedList, okResult.Value);
        }

        // --- 3. RegularProductController Tests ---

        [Fact]
        public async Task RegularController_Create_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new RegularProductController(_serviceMock.Object);
            var dto = new ProductCreateDto { Name = "R1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Create(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task RegularController_Update_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new RegularProductController(_serviceMock.Object);
            var dto = new ProductUpdateDto { Id = 1, Name = "R1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Update(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task RegularController_Create_ShouldReturnOk_WhenNoRecipe()
        {
            // Arrange
            var controller = new RegularProductController(_serviceMock.Object);
            var dto = new ProductCreateDto { Name = "R1", Recipe = null };
            var expected = new ProductViewDto { Id = 1, Name = "R1" };
            _serviceMock.Setup(s => s.CreateWithTypeAsync(dto, "regular")).ReturnsAsync(expected);

            // Act
            var result = await controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        // --- 4. IngredientProductController Tests ---

        [Fact]
        public async Task IngredientController_Create_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new IngredientProductController(_serviceMock.Object);
            var dto = new ProductCreateDto { Name = "I1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Create(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task IngredientController_Update_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new IngredientProductController(_serviceMock.Object);
            var dto = new ProductUpdateDto { Id = 1, Name = "I1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Update(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        // --- 5. ToolProductController Tests ---

        [Fact]
        public async Task ToolController_Create_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new ToolProductController(_serviceMock.Object);
            var dto = new ProductCreateDto { Name = "T1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Create(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }

        [Fact]
        public async Task ToolController_Update_ShouldThrowRecipeNotAllowed_WhenRecipeIsProvided()
        {
            // Arrange
            var controller = new ToolProductController(_serviceMock.Object);
            var dto = new ProductUpdateDto { Id = 1, Name = "T1", Recipe = new ProductRecipeDto { VersionName = "v1" } };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => controller.Update(dto));
            Assert.Equal(ErrorCodes.RecipeNotAllowed.Code, ex.Code);
        }
    }
}
