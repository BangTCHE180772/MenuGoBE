using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Unit;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class UnitServiceTests
    {
        private readonly Mock<IUnitRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly UnitService _service;

        public UnitServiceTests()
        {
            _repoMock = new Mock<IUnitRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new UnitService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos_WhenUnitsExist()
        {
            // Arrange
            var units = new List<Unit>
            {
                new Unit { Id = 1, Name = "kg" },
                new Unit { Id = 2, Name = "pcs" }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(units);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("kg", result[0].Name);
            Assert.Equal("pcs", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUnitsExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Unit>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenUnitExists()
        {
            // Arrange
            var unitId = 1L;
            var unit = new Unit { Id = unitId, Name = "kg" };
            _repoMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);

            // Act
            var result = await _service.GetByIdAsync(unitId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("kg", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(unitId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenUnitDoesNotExist()
        {
            // Arrange
            var unitId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync((Unit?)null);

            // Act
            var result = await _service.GetByIdAsync(unitId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(unitId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto_WhenNameIsUnique()
        {
            // Arrange
            var createDto = new UnitCreateDto { Name = "box" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Unit>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("box", result.Name);
            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Unit>(u => u.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicateUnitNameException_WhenNameAlreadyExists()
        {
            // Arrange
            var createDto = new UnitCreateDto { Name = "kg" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.DuplicateUnitName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateUnitName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Unit>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenUnitExistsAndNameIsUnique()
        {
            // Arrange
            var updateDto = new UnitUpdateDto { Id = 1, Name = "kg_updated" };
            var existingUnit = new Unit { Id = 1, Name = "kg" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingUnit);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Unit>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("kg_updated", existingUnit.Name);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingUnit), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenUnitDoesNotExist()
        {
            // Arrange
            var updateDto = new UnitUpdateDto { Id = 99, Name = "non_existent" };
            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Unit?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Unit>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowDuplicateUnitNameException_WhenNameExistsOnAnotherUnit()
        {
            // Arrange
            var updateDto = new UnitUpdateDto { Id = 1, Name = "pcs" };
            var existingUnit = new Unit { Id = 1, Name = "kg" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingUnit);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.DuplicateUnitName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateUnitName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Unit>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenUnitExistsAndIsNotInUse()
        {
            // Arrange
            var unitId = 1L;
            var existingUnit = new Unit { Id = unitId, Name = "kg" };

            _repoMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(existingUnit);
            _repoMock.Setup(r => r.HasConversionsAsync(unitId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.DeleteAsync(unitId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(unitId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.HasConversionsAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenUnitDoesNotExist()
        {
            // Arrange
            var unitId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync((Unit?)null);

            // Act
            var result = await _service.DeleteAsync(unitId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.HasConversionsAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowUnitInUseException_WhenUnitHasConversions()
        {
            // Arrange
            var unitId = 1L;
            var existingUnit = new Unit { Id = unitId, Name = "kg" };

            _repoMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(existingUnit);
            _repoMock.Setup(r => r.HasConversionsAsync(unitId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.DeleteAsync(unitId));
            Assert.Equal(ErrorCodes.UnitInUse.Code, ex.Code);
            Assert.Equal(ErrorCodes.UnitInUse.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.GetByIdAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.HasConversionsAsync(unitId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
