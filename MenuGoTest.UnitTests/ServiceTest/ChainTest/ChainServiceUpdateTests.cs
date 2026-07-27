using System;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.ChainTest
{
    public class ChainServiceUpdateTests
    {
        private readonly Mock<IChainRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ChainService _service;

        public ChainServiceUpdateTests()
        {
            _repoMock = new Mock<IChainRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new ChainService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_True_When_Chain_Exists_And_Is_Updated()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = "Updated Chain",
                OpenTime = new TimeOnly(10, 0),
                CloseTime = new TimeOnly(22, 0),
                LogoImage = "new_logo.png",
                BackgroundImage = "new_bg.png",
                Address = new MenuGoBE.Dtos.Address.AddressUpdateDto()
            };

            var existingChain = new Chain
            {
                Id = 1,
                Name = "Old Chain",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(20, 0),
                LogoImage = "old_logo.png",
                BackgroundImage = "old_bg.png",
                AddressId = 10,
                Address = new Address { Id = 10, Type = "Chuỗi" }
            };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingChain);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Chain>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Updated Chain", existingChain.Name);
            Assert.Equal(updateDto.OpenTime, existingChain.OpenTime);
            Assert.Equal(updateDto.CloseTime, existingChain.CloseTime);
            Assert.Equal(updateDto.LogoImage, existingChain.LogoImage);
            Assert.Equal(updateDto.BackgroundImage, existingChain.BackgroundImage);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingChain), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_False_When_Chain_Does_Not_Exist()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 99,
                Name = "Non-existent Chain",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Chain?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Chain>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task UpdateAsync_ShouldThrowException_WhenNameIsEmptyOrWhitespace(string? invalidName)
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = invalidName!,
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.ChainNameRequired.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = new string('A', 101),
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.ChainNameLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = "Valid Name",
                OpenTime = new TimeOnly(22, 0),
                CloseTime = new TimeOnly(8, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.ChainOpenCloseTimeInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenLogoPathExceeds255Characters()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = "Valid Name",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0),
                LogoImage = new string('L', 256)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenBackgroundPathExceeds255Characters()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 1,
                Name = "Valid Name",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0),
                BackgroundImage = new string('B', 256)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }
    }
}
