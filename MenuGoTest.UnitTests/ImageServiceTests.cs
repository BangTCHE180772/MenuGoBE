using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Image;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class ImageServiceTests
    {
        private readonly Mock<IImageRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ImageService _service;

        public ImageServiceTests()
        {
            _repoMock = new Mock<IImageRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new ImageService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos_WhenImagesExist()
        {
            // Arrange
            var images = new List<Image>
            {
                new Image { Id = 1, ImageLink = "link1.png" },
                new Image { Id = 2, ImageLink = "link2.png" }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(images);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("link1.png", result[0].ImageLink);
            Assert.Equal("link2.png", result[1].ImageLink);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoImagesExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Image>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenImageExists()
        {
            // Arrange
            var imageId = 1L;
            var image = new Image { Id = imageId, ImageLink = "link1.png" };
            _repoMock.Setup(r => r.GetByIdAsync(imageId)).ReturnsAsync(image);

            // Act
            var result = await _service.GetByIdAsync(imageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("link1.png", result.ImageLink);
            _repoMock.Verify(r => r.GetByIdAsync(imageId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenImageDoesNotExist()
        {
            // Arrange
            var imageId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(imageId)).ReturnsAsync((Image?)null);

            // Act
            var result = await _service.GetByIdAsync(imageId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(imageId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto()
        {
            // Arrange
            var createDto = new ImageCreateDto { ImageLink = "new_link.png" };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Image>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new_link.png", result.ImageLink);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Image>(img => img.ImageLink == createDto.ImageLink)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenImageExists()
        {
            // Arrange
            var updateDto = new ImageUpdateDto { Id = 1, ImageLink = "updated_link.png" };
            var existingImage = new Image { Id = 1, ImageLink = "old_link.png" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingImage);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Image>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("updated_link.png", existingImage.ImageLink);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingImage), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenImageDoesNotExist()
        {
            // Arrange
            var updateDto = new ImageUpdateDto { Id = 99, ImageLink = "non_existent.png" };
            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Image?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Image>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenImageExists()
        {
            // Arrange
            var imageId = 1L;
            var existingImage = new Image { Id = imageId, ImageLink = "link.png" };

            _repoMock.Setup(r => r.GetByIdAsync(imageId)).ReturnsAsync(existingImage);
            _repoMock.Setup(r => r.DeleteAsync(imageId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(imageId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(imageId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(imageId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenImageDoesNotExist()
        {
            // Arrange
            var imageId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(imageId)).ReturnsAsync((Image?)null);

            // Act
            var result = await _service.DeleteAsync(imageId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(imageId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
