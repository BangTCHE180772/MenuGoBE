using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Group;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class GroupServiceTests
    {
        private readonly Mock<IGroupRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly GroupService _service;

        public GroupServiceTests()
        {
            _repoMock = new Mock<IGroupRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new GroupService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos_WhenGroupsExist()
        {
            // Arrange
            var groups = new List<Group>
            {
                new Group { Id = 1, Name = "Beverages" },
                new Group { Id = 2, Name = "Desserts" }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(groups);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Beverages", result[0].Name);
            Assert.Equal("Desserts", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoGroupsExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Group>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenGroupExists()
        {
            // Arrange
            var groupId = 1L;
            var group = new Group { Id = groupId, Name = "Beverages" };
            _repoMock.Setup(r => r.GetByIdAsync(groupId)).ReturnsAsync(group);

            // Act
            var result = await _service.GetByIdAsync(groupId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Beverages", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(groupId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenGroupDoesNotExist()
        {
            // Arrange
            var groupId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(groupId)).ReturnsAsync((Group?)null);

            // Act
            var result = await _service.GetByIdAsync(groupId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(groupId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto_WhenNameIsUnique()
        {
            // Arrange
            var createDto = new GroupCreateDto { Name = "Main Course" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Main Course", result.Name);
            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Group>(g => g.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicateGroupNameException_WhenNameAlreadyExists()
        {
            // Arrange
            var createDto = new GroupCreateDto { Name = "Beverages" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.DuplicateGroupName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateGroupName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Group>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenGroupExistsAndNameIsUnique()
        {
            // Arrange
            var updateDto = new GroupUpdateDto { Id = 1, Name = "Beverages Updated" };
            var existingGroup = new Group { Id = 1, Name = "Beverages" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingGroup);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Beverages Updated", existingGroup.Name);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingGroup), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenGroupDoesNotExist()
        {
            // Arrange
            var updateDto = new GroupUpdateDto { Id = 99, Name = "Non Existent" };
            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Group?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowDuplicateGroupNameException_WhenNameExistsOnAnotherGroup()
        {
            // Arrange
            var updateDto = new GroupUpdateDto { Id = 1, Name = "Desserts" };
            var existingGroup = new Group { Id = 1, Name = "Beverages" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingGroup);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.DuplicateGroupName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateGroupName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenGroupExists()
        {
            // Arrange
            var groupId = 1L;
            var existingGroup = new Group { Id = groupId, Name = "Beverages" };

            _repoMock.Setup(r => r.GetByIdAsync(groupId)).ReturnsAsync(existingGroup);
            _repoMock.Setup(r => r.DeleteAsync(groupId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(groupId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(groupId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(groupId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenGroupDoesNotExist()
        {
            // Arrange
            var groupId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(groupId)).ReturnsAsync((Group?)null);

            // Act
            var result = await _service.DeleteAsync(groupId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(groupId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
