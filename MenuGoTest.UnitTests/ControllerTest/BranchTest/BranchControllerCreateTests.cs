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
    public class BranchControllerCreateTests
    {
        private readonly Mock<IBranchService> _serviceMock;
        private readonly BranchController _controller;

        public BranchControllerCreateTests()
        {
            _serviceMock = new Mock<IBranchService>();
            _controller = new BranchController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_Create_Should_Return_CreatedAtAction()
        {
            var dto = new BranchCreateDto { Name = "New Branch" };
            var created = new BranchViewDto { Id = 1, Name = "New Branch" };

            _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

            var result = await _controller.Create(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var data = Assert.IsType<BranchViewDto>(createdResult.Value);
            Assert.Equal(created.Id, data.Id);
            Assert.Equal(created.Name, data.Name);
            Assert.Equal(nameof(BranchController.GetById), createdResult.ActionName);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenNameIsEmptyOrWhitespace()
        {
            var dto = new BranchCreateDto { Name = "" };
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<BranchCreateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchNameRequired));

            var result = await _controller.Create(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenNameExceeds100Characters()
        {
            var dto = new BranchCreateDto { Name = new string('B', 101) };
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<BranchCreateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchNameLengthExceeded));

            var result = await _controller.Create(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            var dto = new BranchCreateDto { Name = "Valid Branch" };
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<BranchCreateDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid));

            var result = await _controller.Create(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }
    }
}
