using System;
using System.Collections.Generic;
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
    public class BranchControllerGetTests
    {
        private readonly Mock<IBranchService> _serviceMock;
        private readonly BranchController _controller;

        public BranchControllerGetTests()
        {
            _serviceMock = new Mock<IBranchService>();
            _controller = new BranchController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_GetAll_Should_Return_Ok_With_List()
        {
            var branches = new List<BranchViewDto>
            {
                new BranchViewDto { Id = 1, Name = "Branch A" },
                new BranchViewDto { Id = 2, Name = "Branch B" }
            };

            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(branches);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<BranchViewDto>>(okResult.Value);
            Assert.Equal(2, data.Count);
            _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_Ok_When_Branch_Exists()
        {
            var branch = new BranchViewDto { Id = 1, Name = "Branch A" };
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(branch);

            var result = await _controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<BranchViewDto>(okResult.Value);
            Assert.Equal(1, data.Id);
            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_NotFound_When_Branch_Does_Not_Exist()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((BranchViewDto?)null);

            var result = await _controller.GetById(1);

            Assert.IsType<NotFoundObjectResult>(result);
            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Search_Should_Return_Ok_With_List()
        {
            var dto = new BranchQueryDto { Keyword = "Royal", Page = 1, PageSize = 10 };
            var branches = new List<BranchViewDto>
            {
                new BranchViewDto { Id = 1, Name = "Royal Coffee" }
            };

            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(dto)).ReturnsAsync(branches);

            var result = await _controller.Search(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<BranchViewDto>>(okResult.Value);
            Assert.Single(data);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnInternalServerError_WhenPageLessThan1()
        {
            var dto = new BranchQueryDto { Page = 0, PageSize = 10 };
            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(It.IsAny<BranchQueryDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchQueryPageInvalid));

            var result = await _controller.Search(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnInternalServerError_WhenPageSizeOutOfInclusiveRange()
        {
            var dto = new BranchQueryDto { Page = 1, PageSize = 101 };
            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(It.IsAny<BranchQueryDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchQueryPageSizeInvalid));

            var result = await _controller.Search(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnInternalServerError_WhenSortByInvalidKey()
        {
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, SortBy = "invalid_key" };
            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(It.IsAny<BranchQueryDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchQuerySortByInvalid));

            var result = await _controller.Search(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnInternalServerError_WhenKeywordExceeds100Characters()
        {
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Keyword = new string('K', 101) };
            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(It.IsAny<BranchQueryDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchQueryKeywordLengthExceeded));

            var result = await _controller.Search(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Search_ShouldReturnInternalServerError_WhenTypeExceeds50Characters()
        {
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Type = new string('T', 51) };
            _serviceMock.Setup(s => s.SearchFilteredBranchesAsync(It.IsAny<BranchQueryDto>()))
                .ThrowsAsync(new MenuGoException(ErrorCodes.BranchQueryTypeLengthExceeded));

            var result = await _controller.Search(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.SearchFilteredBranchesAsync(dto), Times.Once);
        }
    }
}
