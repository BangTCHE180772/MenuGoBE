using System.Threading.Tasks;
using MenuGoBE.Controllers;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ControllerTest.ChainTest
{
    public class ChainControllerDeleteTest
    {
        private readonly Mock<IChainService> _serviceMock;
        private readonly ChainController _controller;

        public ChainControllerDeleteTest()
        {
            _serviceMock = new Mock<IChainService>();
            _controller = new ChainController(_serviceMock.Object);
        }

        [Fact]
        public async Task Test_Delete_Should_Return_Ok_When_Success()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<OkObjectResult>(result);
            _serviceMock.Verify(s => s.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Test_Delete_Should_Return_NotFound_When_Failed()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);

            var result = await _controller.Delete(1);

            Assert.IsType<NotFoundObjectResult>(result);
            _serviceMock.Verify(s => s.DeleteAsync(1), Times.Once);
        }
    }
}
