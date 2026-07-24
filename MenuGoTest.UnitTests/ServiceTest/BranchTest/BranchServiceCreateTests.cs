using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.BranchTest
{
    public class BranchServiceCreateTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;

        public BranchServiceCreateTests()
        {
            _repoMock = new Mock<IBranchRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
        }

        [Fact]
        public async Task Test_CreateAsync()
        {
            // Arrange
            var dto = new BranchCreateDto 
            { 
                Name = "New Branch",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };
            var service = new BranchService(_repoMock.Object, _mapper);

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Branch>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Branch>(b => b.Name == dto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task CreateAsync_ShouldThrowException_WhenNameIsEmptyOrWhitespace(string? invalidName)
        {
            // Arrange
            var dto = new BranchCreateDto
            {
                Name = invalidName!,
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };
            var service = new BranchService(_repoMock.Object, _mapper);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => service.CreateAsync(dto));
            Assert.Equal(ErrorCodes.BranchNameRequired.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Branch>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var dto = new BranchCreateDto
            {
                Name = new string('B', 101),
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };
            var service = new BranchService(_repoMock.Object, _mapper);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => service.CreateAsync(dto));
            Assert.Equal(ErrorCodes.BranchNameLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Branch>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var dto = new BranchCreateDto
            {
                Name = "Valid Name",
                OpenTime = new TimeOnly(22, 0), // 10 PM
                CloseTime = new TimeOnly(8, 0)   // 8 AM
            };
            var service = new BranchService(_repoMock.Object, _mapper);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => service.CreateAsync(dto));
            Assert.Equal(ErrorCodes.BranchOpenCloseTimeInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Branch>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateBInventoryForExistingProducts()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MenuGoBE.Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new MenuGoBE.Data.AppDbContext(options);

            long chainId = 5;
            var prod1 = new Product { Id = 10, ChainId = chainId, Name = "Product 10", Type = MenuGoBE.Models.Enums.ProductType.Ingredient };
            var prod2 = new Product { Id = 20, ChainId = chainId, Name = "Product 20", Type = MenuGoBE.Models.Enums.ProductType.Processed };
            await dbContext.Products.AddRangeAsync(prod1, prod2);
            await dbContext.SaveChangesAsync();

            var service = new BranchService(_repoMock.Object, _mapper, dbContext);

            var dto = new BranchCreateDto
            {
                ChainId = chainId,
                Name = "Branch District 1",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Branch>()))
                .Callback<Branch>(b => b.Id = 50)
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

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
