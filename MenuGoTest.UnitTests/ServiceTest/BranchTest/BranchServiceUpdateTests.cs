using System;
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
    public class BranchServiceUpdateTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly BranchService _service;

        public BranchServiceUpdateTests()
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
        public async Task Test_UpdateAsync_Should_Return_True_When_Branch_Exists()
        {
            // Arrange
            var dto = new BranchUpdateDto 
            { 
                Id = 1, 
                Name = "Updated Branch",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };
            var entity = new Branch 
            { 
                Id = 1, 
                Name = "Old",
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(21, 0),
                Address = new Address { Type = "Chi Nhánh" }
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.Id)).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Branch>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

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
                Id = 99,
                Name = "Non-existent Branch",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };
            _repoMock.Setup(r => r.GetByIdAsync(dto.Id)).ReturnsAsync((Branch?)null);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Branch>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateAsync_ShouldThrowException_WhenNameIsEmptyOrWhitespace(string? invalidName)
        {
            // Arrange
            var dto = new BranchUpdateDto
            {
                Id = 1,
                Name = invalidName!,
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
            Assert.Equal(ErrorCodes.BranchNameRequired.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var dto = new BranchUpdateDto
            {
                Id = 1,
                Name = new string('B', 101),
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
            Assert.Equal(ErrorCodes.BranchNameLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var dto = new BranchUpdateDto
            {
                Id = 1,
                Name = "Valid Name",
                OpenTime = new TimeOnly(22, 0), // 10 PM
                CloseTime = new TimeOnly(8, 0)   // 8 AM
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(dto));
            Assert.Equal(ErrorCodes.BranchOpenCloseTimeInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }
    }
}
