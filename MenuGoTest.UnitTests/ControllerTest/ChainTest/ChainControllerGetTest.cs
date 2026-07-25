using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ControllerTest.ChainTest
{
    public class ChainControllerGetTest
    {
        private readonly Mock<IChainService> _serviceMock;
        private readonly ChainController _controller;

        public ChainControllerGetTest()
        {
            _serviceMock = new Mock<IChainService>();
            _controller = new ChainController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_GetAll_Should_Return_Ok_With_List()
        {
            var chains = new List<ChainViewDto>
            {
                new ChainViewDto { Id = 1, Name = "Chain A" },
                new ChainViewDto { Id = 2, Name = "Chain B" }
            };

            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(chains);

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<List<ChainViewDto>>(okResult.Value);
            Assert.Equal(2, data.Count);
            _serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetMainChain_Should_Return_Ok()
        {
            var chain = new ChainViewDto { Id = 1, Name = "Main Chain" };
            _serviceMock.Setup(s => s.GetMainChainAsync()).ReturnsAsync(chain);

            var result = await _controller.GetMainChain();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<ChainViewDto>(okResult.Value);
            Assert.Equal("Main Chain", data.Name);
            _serviceMock.Verify(s => s.GetMainChainAsync(), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_Ok_When_Exists()
        {
            var chain = new ChainViewDto { Id = 1, Name = "Chain A" };
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(chain);

            var result = await _controller.GetById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<ChainViewDto>(okResult.Value);
            Assert.Equal(1, data.Id);
            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_GetById_Should_Return_NotFound_When_Not_Exists()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((ChainViewDto?)null);

            var result = await _controller.GetById(1);

            Assert.IsType<NotFoundObjectResult>(result);
            _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        }
    }
}
