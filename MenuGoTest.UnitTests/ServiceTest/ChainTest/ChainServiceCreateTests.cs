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
    public class ChainServiceCreateTests
    {
        private readonly Mock<IChainRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ChainService _service;

        public ChainServiceCreateTests()
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
        public async Task CreateAsync_ShouldSucceed_WhenValidDtoProvided()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = "Valid Chain",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0),
                LogoImage = "logo.png",
                BackgroundImage = "bg.png"
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

            _repoMock.Verify(r => r.CreateAsync(It.Is<Chain>(c => c.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task CreateAsync_ShouldThrowException_WhenNameIsEmptyOrWhitespace(string? invalidName)
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = invalidName!,
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.ChainNameRequired.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Chain>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenNameExceeds100Characters()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = new string('A', 101),
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.ChainNameLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Chain>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenOpenTimeIsGreaterThanOrEqualToCloseTime()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = "Valid Name",
                OpenTime = new TimeOnly(22, 0), // 10 PM
                CloseTime = new TimeOnly(8, 0)  // 8 AM
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.ChainOpenCloseTimeInvalid.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Chain>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenLogoPathExceeds255Characters()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = "Valid Name",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0),
                LogoImage = new string('L', 256),
                BackgroundImage = "bg.png"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Chain>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenBackgroundPathExceeds255Characters()
        {
            // Arrange
            var createDto = new ChainCreateDto
            {
                Name = "Valid Name",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0),
                LogoImage = "logo.png",
                BackgroundImage = new string('B', 256)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.ChainImageLengthExceeded.Code, ex.Code);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Chain>()), Times.Never);
        }
    }
}
