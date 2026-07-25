using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.BranchTest
{
    public class BranchServiceGetTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly BranchService _service;

        public BranchServiceGetTests()
        {
            _repoMock = new Mock<IBranchRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new BranchService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task Test_Return_Dtos_Of_GetAllAsync()
        {
            // Arrange
            var branches = new List<Branch>
            {
                new Branch { Id = 1, Name = "Branch A" },
                new Branch { Id = 2, Name = "Branch B" }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(branches);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Branch A", result[0].Name);
            Assert.Equal("Branch B", result[1].Name);

            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Dto_Of_GetByIdAsync_When_Branch_Exists()
        {
            // Arrange
            var branch = new Branch { Id = 1, Name = "Branch A" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(branch);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(branch.Name, result.Name);

            _repoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Null_Of_GetByIdAsync_When_Branch_Does_Not_Exist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Branch?)null);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.Null(result);

            _repoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Dtos_Of_SearchFilteredBranchesAsync()
        {
            // Arrange
            var dto = new BranchQueryDto { Keyword = "Branch", Page = 1, PageSize = 10 };
            var branches = new List<Branch>
            {
                new Branch { Id = 1, Name = "Branch A" },
                new Branch { Id = 2, Name = "Branch B" }
            };

            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(branches);

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenPageGreaterThan1()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 2, PageSize = 10 };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenPageSizeInInclusiveRange(int pageSize)
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = pageSize };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("createdat")]
        [InlineData("id")]
        [InlineData("managerName")]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenSortByAscending(string sortBy)
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, SortBy = sortBy, Desc = false };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("createdat")]
        [InlineData("id")]
        [InlineData("managerName")]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenSortByDescending(string sortBy)
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, SortBy = sortBy, Desc = true };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenKeywordLengthIsValid()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Keyword = new string('K', 100) };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldSucceed_WhenTypeLengthIsValid()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Type = new string('T', 50) };
            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldThrowException_WhenPageLessThan1()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 0, PageSize = 10 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.SearchFilteredBranchesAsync(dto));
            Assert.Equal(ErrorCodes.BranchQueryPageInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(-5)]
        public async Task SearchFilteredBranchesAsync_ShouldThrowException_WhenPageSizeOutOfInclusiveRange(int invalidPageSize)
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = invalidPageSize };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.SearchFilteredBranchesAsync(dto));
            Assert.Equal(ErrorCodes.BranchQueryPageSizeInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Never);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldThrowException_WhenSortByInvalidKey()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, SortBy = "invalid_column" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.SearchFilteredBranchesAsync(dto));
            Assert.Equal(ErrorCodes.BranchQuerySortByInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Never);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldThrowException_WhenKeywordExceeds100Characters()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Keyword = new string('K', 101) };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.SearchFilteredBranchesAsync(dto));
            Assert.Equal(ErrorCodes.BranchQueryKeywordLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Never);
        }

        [Fact]
        public async Task SearchFilteredBranchesAsync_ShouldThrowException_WhenTypeExceeds50Characters()
        {
            // Arrange
            var dto = new BranchQueryDto { Page = 1, PageSize = 10, Type = new string('T', 51) };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.SearchFilteredBranchesAsync(dto));
            Assert.Equal(ErrorCodes.BranchQueryTypeLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Never);
        }
    }
}
