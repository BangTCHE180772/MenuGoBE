using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MenuGoTest.UnitTests
{
    public class ChainControllerTest
    {
        private readonly Mock<IChainService> _serviceMock;
        private readonly ChainController _controller;
        public ChainControllerTest()
        {
            _serviceMock = new Mock<IChainService>();
            _controller = new ChainController(_serviceMock.Object);
        }

        // --- 1. ManufacturedProductController Tests ---
        [Fact]
        public async Task Test_GetAll_Should_Return_Ok_With_List()
        {
            // Arrange
            var chains = new List<ChainViewDto>
                        {
                            new ChainViewDto { Id = 1, Name = "Chain A" },
                            new ChainViewDto { Id = 2, Name = "Chain B" }
                        };

            _serviceMock.Setup(s => s.GetAllAsync())
                .ReturnsAsync(chains);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsAssignableFrom<List<ChainViewDto>>(okResult.Value);

            Assert.Equal(2, data.Count);

            _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetMainChain_Should_Return_Ok()
        {
            // Arrange
            var chain = new ChainViewDto
            {
                Id = 1,
                Name = "Main Chain"
            };

            _serviceMock.Setup(s => s.GetMainChainAsync())
                .ReturnsAsync(chain);

            // Act
            var result = await _controller.GetMainChain();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<ChainViewDto>(okResult.Value);

            Assert.Equal("Main Chain", data.Name);

            _serviceMock.Verify(s => s.GetMainChainAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_Ok_When_Exists()
        {
            // Arrange
            var chain = new ChainViewDto
            {
                Id = 1,
                Name = "Chain A"
            };

            _serviceMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(chain);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<ChainViewDto>(okResult.Value);

            Assert.Equal(1, data.Id);

            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_NotFound_When_Not_Exists()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((ChainViewDto?)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);

            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Create_Should_Return_Ok()
        {
            // Arrange
            var dto = new ChainCreateDto
            {
                Name = "New Chain"
            };

            var created = new ChainViewDto
            {
                Id = 1,
                Name = "New Chain"
            };

            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<ChainViewDto>(okResult.Value);

            Assert.Equal("New Chain", data.Name);

            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Update_Should_Return_Ok_When_Success()
        {
            // Arrange
            var dto = new ChainUpdateDto
            {
                Id = 1,
                Name = "Updated"
            };

            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Update_Should_Return_NotFound_When_Failed()
        {
            // Arrange
            var dto = new ChainUpdateDto
            {
                Id = 1
            };

            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);

            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Delete_Should_Return_Ok_When_Success()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _serviceMock.Verify(s => s.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Delete_Should_Return_NotFound_When_Failed()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);

            _serviceMock.Verify(s => s.DeleteAsync(1), Times.Once);
        }
    }
}