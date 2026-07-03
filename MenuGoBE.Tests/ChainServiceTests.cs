using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoBE.Tests
{
    public class ChainServiceTests
    {
        private readonly Mock<IChainRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ChainService _service;

        public ChainServiceTests()
        {
            _repoMock = new Mock<IChainRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            });
            _mapper = config.CreateMapper();

            _service = new ChainService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnListOfChainViewDtos_WhenChainsExist()
        {
            // Arrange
            var chains = new List<Chain>
            {
                new Chain { Id = 1, Name = "Chain A", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(22, 0) },
                new Chain { Id = 2, Name = "Chain B", OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(23, 0) }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(chains);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Chain A", result[0].Name);
            Assert.Equal("Chain B", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnChainViewDto_WhenChainExists()
        {
            // Arrange
            var chainId = 1L;
            var chain = new Chain { Id = chainId, Name = "Chain A", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(22, 0) };
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync(chain);

            // Act
            var result = await _service.GetByIdAsync(chainId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Chain A", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenChainDoesNotExist()
        {
            // Arrange
            var chainId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync((Chain?)null);

            // Act
            var result = await _service.GetByIdAsync(chainId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCallCreateAndSave_AndReturnMappedDto()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = "New Chain",
                OpenTime = new TimeOnly(7, 0),
                CloseTime = new TimeOnly(21, 0),
                LogoImage = "logo.png",
                BackgroundImage = "bg.png",
                AddressId = 10
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Chain>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.OpenTime, result.OpenTime);
            Assert.Equal(createDto.CloseTime, result.CloseTime);
            Assert.Equal(createDto.LogoImage, result.LogoImage);
            Assert.Equal(createDto.BackgroundImage, result.BackgroundImage);
            Assert.Equal(createDto.AddressId, result.AddressId);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Chain>(c => c.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenChainExistsAndIsUpdated()
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
                AddressId = 15
            };

            var existingChain = new Chain
            {
                Id = 1,
                Name = "Old Chain",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(20, 0),
                LogoImage = "old_logo.png",
                BackgroundImage = "old_bg.png",
                AddressId = 10
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
            Assert.Equal(updateDto.AddressId, existingChain.AddressId);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingChain), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenChainDoesNotExist()
        {
            // Arrange
            var updateDto = new ChainUpdateDto
            {
                Id = 99,
                Name = "Non-existent Chain"
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

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenChainExistsAndIsDeleted()
        {
            // Arrange
            var chainId = 1L;
            var existingChain = new Chain { Id = chainId, Name = "Chain to Delete" };

            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync(existingChain);
            _repoMock.Setup(r => r.DeleteAsync(chainId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(chainId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenChainDoesNotExist()
        {
            // Arrange
            var chainId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync((Chain?)null);

            // Act
            var result = await _service.DeleteAsync(chainId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
