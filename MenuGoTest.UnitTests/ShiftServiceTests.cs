using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Shift;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class ShiftServiceTests
    {
        private readonly Mock<IShiftRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ShiftService _service;

        public ShiftServiceTests()
        {
            _repoMock = new Mock<IShiftRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new ShiftService(_repoMock.Object, _mapper);
        }

        [Fact]
        public async Task Test_Return_Dtos_Of_GetAllAsync()
        {
            // Arrange
            var shifts = new List<Shift>
            {
                new Shift { Id = 1, Name = "Ca Sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), IsActive = true },
                new Shift { Id = 2, Name = "Ca Chiều", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(17, 0), IsActive = true }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(shifts);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Ca Sáng", result[0].Name);
            Assert.Equal("Ca Chiều", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Dto_Of_GetByIdAsync_When_Shift_Exists()
        {
            // Arrange
            var id = 1L;
            var shift = new Shift { Id = id, Name = "Ca Sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shift);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Ca Sáng", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Null_Of_GetByIdAsync_When_Shift_Does_Not_Exist()
        {
            // Arrange
            var id = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Shift?)null);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task Test_CreateAsync()
        {
            // Arrange
            var createDto = new ShiftCreateDto
            {
                Name = "Ca Tối",
                StartTime = new TimeOnly(18, 0),
                EndTime = new TimeOnly(22, 0),
                IsActive = true,
                CreatedBy = 1
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Shift>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.StartTime, result.StartTime);
            Assert.Equal(createDto.EndTime, result.EndTime);
            Assert.True(result.IsActive);
            Assert.Equal(createDto.CreatedBy, result.CreatedBy);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Shift>(s => s.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_True_When_Shift_Exists()
        {
            // Arrange
            var updateDto = new ShiftUpdateDto
            {
                Id = 1,
                Name = "Ca Sáng Updated",
                StartTime = new TimeOnly(7, 30),
                EndTime = new TimeOnly(11, 30),
                IsActive = true
            };

            var existingShift = new Shift
            {
                Id = 1,
                Name = "Ca Sáng",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                IsActive = true
            };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingShift);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Shift>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Ca Sáng Updated", existingShift.Name);
            Assert.Equal(updateDto.StartTime, existingShift.StartTime);
            Assert.Equal(updateDto.EndTime, existingShift.EndTime);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingShift), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_UpdateAsync_Should_Return_False_When_Shift_Does_Not_Exist()
        {
            // Arrange
            var updateDto = new ShiftUpdateDto { Id = 99, Name = "No Exist" };
            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Shift?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Shift>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Test_DeleteAsync_Should_Return_True_When_Shift_Exists()
        {
            // Arrange
            var id = 1L;
            var existingShift = new Shift { Id = id, Name = "Ca Delete" };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingShift);
            _repoMock.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_DeleteAsync_Should_Return_False_When_Shift_Does_Not_Exist()
        {
            // Arrange
            var id = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Shift?)null);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
