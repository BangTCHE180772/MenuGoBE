using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using MenuGoBE.Dtos.Branch;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MenuGoTest.UnitTests
{
    public class BranchServiceTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly BranchService _service;

        public BranchServiceTests()
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
                                new Branch
                                {
                                    Id = 1,
                                    Name = "Branch A",
                                },
                                new Branch
                                {
                                    Id = 2,
                                    Name = "Branch B",
                                }
                            };

            _repoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(branches);

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
            var branch = new Branch
            {
                Id = 1,
                Name = "Branch A",
            };

            _repoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(branch);

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
            _repoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Branch?)null);

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
            var dto = new BranchQueryDto
            {
                Keyword = "Branch"
            };

            var branches = new List<Branch>
                            {
                                new Branch
                                {
                                    Id = 1,
                                    Name = "Branch A",
                                },
                                new Branch
                                {
                                    Id = 2,
                                    Name = "Branch B",
                                }
                            };

            _repoMock.Setup(r => r.SearchFilteredBranchesAsync(dto))
                .ReturnsAsync(branches);

            // Act
            var result = await _service.SearchFilteredBranchesAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _repoMock.Verify(r => r.SearchFilteredBranchesAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Test_CreateAsync()
        {
            // Arrange
            var dto = new BranchCreateDto
            {
                Name = "New Branch",
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Branch>()))
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);


            _repoMock.Verify(r => r.CreateAsync(It.Is<Branch>(b =>
                b.Name == dto.Name)), Times.Once);

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_True_When_Branch_Exists()
        {
            // Arrange
            var dto = new BranchUpdateDto
            {
                Id = 1,
                Name = "Updated Branch",
            };

            var entity = new Branch
            {
                Id = 1,
                Name = "Old",
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.Id))
                .ReturnsAsync(entity);

            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Branch>()))
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(dto.Name, entity.Name);


            _repoMock.Verify(r => r.UpdateAsync(entity), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_False_When_Branch_Does_Not_Exist()
        {
            // Arrange
            var dto = new BranchUpdateDto
            {
                Id = 99
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.Id))
                .ReturnsAsync((Branch?)null);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Branch>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Test_DeleteAsync_Should_Return_True_When_Branch_Exists()
        {
            // Arrange
            var entity = new Branch
            {
                Id = 1,
                Name = "Branch"
            };

            _repoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(entity);

            _repoMock.Setup(r => r.DeleteAsync(1))
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);

            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateBInventoryForExistingProducts()
        {
            // Arrange
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MenuGoBE.Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new MenuGoBE.Data.AppDbContext(options);

            long chainId = 5;
            var prod1 = new Product { Id = 10, ChainId = chainId, Name = "Product 10", Type = "ingredient" };
            var prod2 = new Product { Id = 20, ChainId = chainId, Name = "Product 20", Type = "processed" };
            await dbContext.Products.AddRangeAsync(prod1, prod2);
            await dbContext.SaveChangesAsync();

            var service = new BranchService(_repoMock.Object, _mapper, dbContext);

            var dto = new BranchCreateDto
            {
                ChainId = chainId,
                Name = "Branch District 1"
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Branch>()))
                .Callback<Branch>(b => b.Id = 50)
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            var bInventories = await dbContext.BInventories.Where(bi => bi.BranchId == 50).ToListAsync();
            Assert.Equal(2, bInventories.Count);
            Assert.All(bInventories, bi =>
            {
                Assert.False(bi.ChainActive);
                Assert.False(bi.BranchActive);
                Assert.Equal(0, bi.Quantity);
                Assert.Equal(0, bi.LeftOver);
                Assert.Equal(0, bi.Avg);
            });
        }
    }
}