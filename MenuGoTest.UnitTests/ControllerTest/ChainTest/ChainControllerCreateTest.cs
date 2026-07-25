using System;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ControllerTest.ChainTest
{
    public class ChainControllerCreateTest
    {
        private readonly Mock<IChainService> _serviceMock;
        private readonly ChainController _controller;

        public ChainControllerCreateTest()
        {
            _serviceMock = new Mock<IChainService>();
            _controller = new ChainController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_Create_Should_Return_Ok()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = "New Chain" };
            var created = new ChainViewDto { Id = 1, Name = "New Chain" };

            _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<ChainViewDto>(okResult.Value);
            Assert.Equal("New Chain", data.Name);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenNameIsEmptyOrWhitespace()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = "" };
            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainNameRequired));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Create(dto));
            Assert.Equal(ErrorCodes.ChainNameRequired.Code, ex.Code);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = new string('A', 101) };
            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainNameLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Create(dto));
            Assert.Equal(ErrorCodes.ChainNameLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = "Valid Name" };
            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainOpenCloseTimeInvalid));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Create(dto));
            Assert.Equal(ErrorCodes.ChainOpenCloseTimeInvalid.Code, ex.Code);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenLogoPathExceeds255Characters()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = "Valid Name", LogoImage = new string('L', 256) };
            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainImageLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Create(dto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenBackgroundPathExceeds255Characters()
        {
            // Arrange
            var dto = new ChainCreateDto { Name = "Valid Name", BackgroundImage = new string('B', 256) };
            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainImageLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Create(dto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }
    }
}
