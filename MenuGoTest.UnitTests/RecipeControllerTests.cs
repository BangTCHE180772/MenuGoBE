using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class RecipeControllerTests
    {
        private readonly Mock<IRecipeService> _serviceMock;
        private readonly RecipeController _controller;

        public RecipeControllerTests()
        {
            _serviceMock = new Mock<IRecipeService>();
            _controller = new RecipeController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk()
        {
            // Arrange
            var expectedList = new List<ProductRecipeViewDto> { new ProductRecipeViewDto { Id = 1, VersionName = "v1" } };
            _serviceMock.Setup(s => s.GetAllByProductAsync(10)).ReturnsAsync(expectedList);

            // Act
            var result = await _controller.GetAll(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedList, okResult.Value);
        }

        [Fact]
        public async Task Create_ShouldReturnOk()
        {
            // Arrange
            var dto = new ProductRecipeDto { VersionName = "v1" };
            var expected = new ProductRecipeViewDto { Id = 1, VersionName = "v1" };
            _serviceMock.Setup(s => s.CreateAsync(10, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.Create(10, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task Update_ShouldReturnOk()
        {
            // Arrange
            var dto = new ProductRecipeDto { VersionName = "v1" };
            var expected = new ProductRecipeViewDto { Id = 1, VersionName = "v1" };
            _serviceMock.Setup(s => s.UpdateAsync(10, 1, dto)).ReturnsAsync(expected);

            // Act
            var result = await _controller.Update(10, 1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task Delete_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(10, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(10, 1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(10, 1)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(10, 1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
