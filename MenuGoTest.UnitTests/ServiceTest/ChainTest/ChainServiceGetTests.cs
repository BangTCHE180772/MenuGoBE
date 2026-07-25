using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

//dotnet test MenuGoBE.slnx để chạy test 
namespace MenuGoTest.UnitTests.ServiceTest.ChainTest
{
    public class ChainServiceGetTests
    {
        private readonly Mock<IChainRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ChainService _service;

        public ChainServiceGetTests()
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
        public async Task Test_Return_Dtos_Of_GetAllAsync()
        {
            var chains = new List<Chain>
            {
                new Chain { Id = 1, Name = "Chain A", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(22, 0) },
                new Chain { Id = 2, Name = "Chain B", OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(23, 0) }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(chains);

            var result = await _service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Chain A", result[0].Name);
            Assert.Equal("Chain B", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Dto_Of_GetMainChainAsync_When_Chain_Exists()
        {
            var chain = new Chain
            {
                Id = 1,
                Name = "Main Chain",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(22, 0)
            };

            _repoMock.Setup(r => r.GetMainChainAsync()).ReturnsAsync(chain);

            var result = await _service.GetMainChainAsync();

            Assert.NotNull(result);
            Assert.Equal(chain.Id, result.Id);
            Assert.Equal(chain.Name, result.Name);
            Assert.Equal(chain.OpenTime, result.OpenTime);
            Assert.Equal(chain.CloseTime, result.CloseTime);

            _repoMock.Verify(r => r.GetMainChainAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Dtos_Of_GetByIdAsync_When_Chain_Exists()
        {
            var chainId = 1L;
            var chain = new Chain { Id = chainId, Name = "Chain A", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(22, 0) };
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync(chain);

            var result = await _service.GetByIdAsync(chainId);

            Assert.NotNull(result);
            Assert.Equal("Chain A", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
        }

        [Fact]
        public async Task Test_Return_Null_Of_GetByIdAsync_When_Chain_Does_Not_Exist()
        {
            var chainId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(chainId)).ReturnsAsync((Chain?)null);

            var result = await _service.GetByIdAsync(chainId);

            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(chainId), Times.Once);
        }
    }
}
