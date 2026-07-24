using System;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ControllerTest.BranchTest
{
    public class BranchControllerUpdateTests
    {
        private readonly Mock<IBranchService> _serviceMock;
        private readonly BranchController _controller;

        public BranchControllerUpdateTests()
        {
            _serviceMock = new Mock<IBranchService>();
            _controller = new BranchController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_Update_Should_Return_Ok_When_Success()
        {
            var dto = new BranchUpdateDto { Id = 1, Name = "Updated" };
            _serviceMock.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(true);

            var result = await _controller.Update(dto);

            Assert.IsType<OkObjectResult>(result);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Update_Should_Return_NotFound_When_Failed()
        {
            var dto = new BranchUpdateDto { Id = 99 };
            _serviceMock.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(false);

            var result = await _controller.Update(dto);

            Assert.IsType<NotFoundObjectResult>(result);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenNameIsEmptyOrWhitespace()
        {
            var dto = new BranchUpdateDto { Id = 1, Name = "" };
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<BranchUpdateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchNameRequired));

            var result = await _controller.Update(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenNameExceeds100Characters()
        {
            var dto = new BranchUpdateDto { Id = 1, Name = new string('B', 101) };
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<BranchUpdateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchNameLengthExceeded));

            var result = await _controller.Update(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            var dto = new BranchUpdateDto { Id = 1, Name = "Valid Branch" };
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<BranchUpdateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid));

            var result = await _controller.Update(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.UpdateAsync(dto), Times.Once);
        }
    }
}
