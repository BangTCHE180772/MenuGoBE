using System;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.ChainTest
{
    public class ChainServiceDeleteTests
    {
        private readonly Mock<IChainRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ChainService _service;

        public ChainServiceDeleteTests()
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
        public async Task Test_DeleteAsync_Should_Return_True_When_Chain_Exists_And_Is_Deleted()
        {
            var chainId = 1L;
            var existingChain = new Chain { Id = chainId, Name = "Chain to Delete" };

            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync(existingChain);
            _repoMock.Setup(r => r.DeleteAsync(chainId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(chainId);

            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_DeleteAsync_Should_Return_False_When_Chain_Does_Not_Exist()
        {
            var chainId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync((Chain?)null);

            var result = await _service.DeleteAsync(chainId);

            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
