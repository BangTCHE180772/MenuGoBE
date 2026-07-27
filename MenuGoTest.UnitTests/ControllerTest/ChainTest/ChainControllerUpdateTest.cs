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
    public class ChainControllerUpdateTest
    {
        private readonly Mock<IChainService> _serviceMock;
        private readonly ChainController _controller;

        public ChainControllerUpdateTest()
        {
            _serviceMock = new Mock<IChainService>();
            _controller = new ChainController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_Update_Should_Return_Ok_When_Success()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = "Updated" };
            _serviceMock.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(true);

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
            var dto = new ChainUpdateDto { Id = 1 };
            _serviceMock.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenNameIsEmptyOrWhitespace()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = "" };
            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainNameRequired));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Update(dto));
            Assert.Equal(ErrorCodes.ChainNameRequired.Code, ex.Code);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = new string('A', 101) };
            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainNameLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Update(dto));
            Assert.Equal(ErrorCodes.ChainNameLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = "Valid Name" };
            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainOpenCloseTimeInvalid));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Update(dto));
            Assert.Equal(ErrorCodes.ChainOpenCloseTimeInvalid.Code, ex.Code);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenLogoPathExceeds255Characters()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = "Valid Name", LogoImage = new string('L', 256) };
            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainImageLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Update(dto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenBackgroundPathExceeds255Characters()
        {
            // Arrange
            var dto = new ChainUpdateDto { Id = 1, Name = "Valid Name", BackgroundImage = new string('B', 256) };
            _serviceMock.Setup(s => s.UpdateAsync(dto))
                .ThrowsAsync(new MenuGoException(ErrorCodes.ChainImageLengthExceeded));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _controller.Update(dto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }
    }
}
