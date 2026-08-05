using System;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.BranchTest
{
    public class BranchServiceDeleteTests
    {
        private readonly Mock<IBranchRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly BranchService _service;

        public BranchServiceDeleteTests()
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
        public async Task Test_DeleteAsync_Should_Return_True_When_Branch_Exists()
        {
            // Arrange
            var entity = new Branch { Id = 1, Name = "Branch" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Branch>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            Assert.True(entity.IsDeleted);
            Assert.Equal("Ngừng kinh doanh", entity.Status);

            _repoMock.Verify(r => r.UpdateAsync(entity), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_DeleteAsync_Should_Return_False_When_Branch_Does_Not_Exist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Branch?)null);

            // Act
            var result = await _service.DeleteAsync(99);

            // Assert
            Assert.False(result);

            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
