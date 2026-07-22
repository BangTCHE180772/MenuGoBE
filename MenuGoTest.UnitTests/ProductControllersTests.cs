using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class ProductControllersTests
    {
        private readonly Mock<IProductService> _serviceMock;
        private readonly ProductController _controller;

        public ProductControllersTests()
        {
            _serviceMock = new Mock<IProductService>();
            _controller = new ProductController(_serviceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task ProductController_GetAll_ShouldReturnOk()
        {
            // Arrange
            var expectedList = new List<ProductViewDto> { new ProductViewDto { Id = 1, Name = "P1", Type = ProductType.Processed } };
            _serviceMock.Setup(s => s.GetAllAsync(ProductType.Processed)).ReturnsAsync(expectedList);

            // Act
            var result = await _controller.GetAll(ProductType.Processed);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedList, okResult.Value);
        }

        [Fact]
        public async Task ProductController_GetById_ShouldReturnOk_WhenExists()
        {
            // Arrange
            var expectedProduct = new ProductViewDto { Id = 1, Name = "M1", Type = ProductType.Manufactured };
            _serviceMock.Setup(s => s.GetByIdAndTypeAsync(1, ProductType.Manufactured)).ReturnsAsync(expectedProduct);

            // Act
            var result = await _controller.GetById(1, ProductType.Manufactured);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedProduct, okResult.Value);
        }

        [Fact]
        public async Task ProductController_GetById_ShouldReturnNotFound_WhenNotExists()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAndTypeAsync(99, ProductType.Regular)).ReturnsAsync((ProductViewDto?)null);

            // Act
            var result = await _controller.GetById(99, ProductType.Regular);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ProductController_Create_ShouldReturnOk()
        {
            // Arrange
            var dto = new ProductCreateDto { Name = "New Tool", Type = ProductType.Tool };
            var created = new ProductViewDto { Id = 10, Name = "New Tool", Type = ProductType.Tool };
            _serviceMock.Setup(s => s.CreateAsync(dto, null)).ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(created, okResult.Value);
        }

        [Fact]
        public async Task ProductController_Update_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var dto = new ProductUpdateDto { Id = 1, Name = "Updated" };
            _serviceMock.Setup(s => s.UpdateAsync(dto, null)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto, 1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ProductController_Update_ShouldReturnNotFound_WhenFailed()
        {
            // Arrange
            var dto = new ProductUpdateDto { Id = 99, Name = "NonExistent" };
            _serviceMock.Setup(s => s.UpdateAsync(dto, null)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(dto, 99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ProductController_Delete_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(1, null)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ProductController_Delete_ShouldReturnNotFound_WhenFailed()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(99, null)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
