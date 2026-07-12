using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MenuGoTest.UnitTests
{
    public class BranchControllerTests
    {
        private readonly Mock<IBranchService> _serviceMock;
        private readonly BranchController _controller;

        public BranchControllerTests()
        {
            _serviceMock = new Mock<IBranchService>();
            _controller = new BranchController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_GetAll_Should_Return_Ok_With_List()
        {
            // Arrange
            var branches = new List<BranchViewDto>
                            {
                                new BranchViewDto { Id = 1, Name = "Branch A" },
                                new BranchViewDto { Id = 2, Name = "Branch B" }
                            };

            _serviceMock.Setup(s => s.GetAllAsync())
                .ReturnsAsync(branches);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<List<BranchViewDto>>(okResult.Value);

            Assert.Equal(2, data.Count);

            _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_Ok_When_Branch_Exists()
        {
            // Arrange
            var branch = new BranchViewDto
            {
                Id = 1,
                Name = "Branch A"
            };

            _serviceMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(branch);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<BranchViewDto>(okResult.Value);

            Assert.Equal(1, data.Id);

            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_NotFound_When_Branch_Does_Not_Exist()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((BranchViewDto?)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);

            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Search_Should_Return_Ok_With_List()
        {
            // Arrange
            var dto = new BranchQueryDto
            {
                Keyword = "Royal"
            };

            var branches = new List<BranchViewDto>
                            {
                                new BranchViewDto
                                {
                                    Id = 1,
                                    Name = "Royal Coffee"
                                }
                            };

            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(dto))
                .ReturnsAsync(branches);

            // Act
            var result = await _controller.Search(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var data = Assert.IsType<List<BranchViewDto>>(okResult.Value);

            Assert.Single(data);

            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Create_Should_Return_CreatedAtAction()
        {
            // Arrange
            var dto = new BranchCreateDto
            {
                Name = "New Branch"
            };

            var created = new BranchViewDto
            {
                Id = 1,
                Name = "New Branch"
            };

            _serviceMock.Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);

            var data = Assert.IsType<BranchViewDto>(createdResult.Value);

            Assert.Equal(created.Id, data.Id);
            Assert.Equal(created.Name, data.Name);

            Assert.Equal(nameof(BranchController.GetById), createdResult.ActionName);

            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_Update_Should_Return_Ok_When_Success()
        {
            // Arrange
            var dto = new BranchUpdateDto
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
            var dto = new BranchUpdateDto
            {
                Id = 99
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