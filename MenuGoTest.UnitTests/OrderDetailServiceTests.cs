using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class OrderDetailServiceTests
    {
        private readonly Mock<IOrderDetailRepository> _detailRepoMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<MenuGoBE.Interface.Services.ILeftoverRecordService> _leftoverServiceMock;
        private readonly OrderDetailService _service;

        public OrderDetailServiceTests()
        {
            _detailRepoMock = new Mock<IOrderDetailRepository>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _mapperMock = new Mock<IMapper>();
            _leftoverServiceMock = new Mock<MenuGoBE.Interface.Services.ILeftoverRecordService>();

            _service = new OrderDetailService(
                _detailRepoMock.Object,
                _orderRepoMock.Object,
                _mapperMock.Object,
                new Mock<MenuGoBE.Interface.Repository.ITableRepository>().Object,
                new Mock<MenuGoBE.Interface.Repository.IProductRepository>().Object,
                new Mock<MenuGoBE.Interface.Repository.IAreaRepository>().Object,
                new Mock<MenuGoBE.Interface.Services.IKitchenService>().Object,
                new Mock<MenuGoBE.Interface.Repository.IBInventoryRepository>().Object,
                new Mock<MenuGoBE.Interface.Repository.IRecipesDetailedRepository>().Object,
                new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object,
                new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>().Object,
                _leftoverServiceMock.Object);
        }

        [Fact]
        public async Task GetKitchenItemsAsync_ShouldReturnActiveKitchenItems_WithProductImage()
        {
            // Arrange
            var image = new Image
            {
                Id = 1,
                ImageLink = "http://example.com/pho.jpg"
            };

            var product = new Product
            {
                Id = 10,
                Name = "Phở bò",
                ImageId = 1,
                Image = image,
                Type = MenuGoBE.Models.Enums.ProductType.Processed
            };

            var order = new Order
            {
                Id = 100,
                TableId = 5,
                Table = new Table { Id = 5, Name = "Bàn 5" },
                Status = "Active",
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        Id = 200,
                        OrderId = 100,
                        ProductId = 10,
                        Product = product,
                        Quantity = 2,
                        Price = 50000,
                        Note = "Không hành",
                        Status = "Confirmed",
                        CookingStatus = "Waiting",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            _orderRepoMock
                .Setup(r => r.GetActiveKitchenOrdersWithDetailsAsync())
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _service.GetKitchenItemsAsync();

            // Assert
            Assert.NotNull(result);
            var item = Assert.Single(result);
            Assert.Equal(200, item.OrderDetailId);
            Assert.Equal(100, item.OrderId);
            Assert.Equal("Bàn 5", item.TableName);
            Assert.Equal("Phở bò", item.ProductName);
            Assert.Equal("http://example.com/pho.jpg", item.ProductImage);
            Assert.Equal(2, item.Quantity);
            Assert.Equal("Waiting", item.CookingStatus);
        }

        [Theory]
        [InlineData("Waiting")]
        [InlineData("Accepted")]
        public async Task ApplyLeftoverReuseAsync_ShouldMarkUsedAndSetReady_WhenDetailIsWaitingOrAccepted(string cookingStatus)
        {
            // Arrange
            var detail = new OrderDetail
            {
                Id = 200,
                ProductId = 10,
                Quantity = 2,
                CookingStatus = cookingStatus
            };

            _detailRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(detail);
            _leftoverServiceMock
                .Setup(s => s.MarkUsedAsync(999, 10, 2, 200))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApplyLeftoverReuseAsync(200, 999);

            // Assert
            Assert.True(result);
            Assert.Equal("Ready", detail.CookingStatus);
            _leftoverServiceMock.Verify(s => s.MarkUsedAsync(999, 10, 2, 200), Times.Once);
            _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ApplyLeftoverReuseAsync_ShouldReturnFalse_WhenDetailNotFound()
        {
            // Arrange
            _detailRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync((OrderDetail?)null);

            // Act
            var result = await _service.ApplyLeftoverReuseAsync(200, 999);

            // Assert
            Assert.False(result);
            _leftoverServiceMock.Verify(s => s.MarkUsedAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<long>()), Times.Never);
        }

        [Theory]
        [InlineData("Cooking")]
        [InlineData("Ready")]
        public async Task ApplyLeftoverReuseAsync_ShouldThrow_WhenDetailAlreadyPastWaiting(string cookingStatus)
        {
            // Arrange
            var detail = new OrderDetail
            {
                Id = 200,
                ProductId = 10,
                Quantity = 2,
                CookingStatus = cookingStatus
            };
            _detailRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(detail);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyLeftoverReuseAsync(200, 999));
            _leftoverServiceMock.Verify(s => s.MarkUsedAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<long>()), Times.Never);
            Assert.Equal(cookingStatus, detail.CookingStatus);
        }

        [Fact]
        public async Task ApplyLeftoverReuseAsync_ShouldNotChangeStatus_WhenMarkUsedAsyncThrows()
        {
            // Arrange
            var detail = new OrderDetail
            {
                Id = 200,
                ProductId = 10,
                Quantity = 2,
                CookingStatus = "Waiting"
            };
            _detailRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(detail);
            _leftoverServiceMock
                .Setup(s => s.MarkUsedAsync(999, 10, 2, 200))
                .ThrowsAsync(new MenuGoBE.Exceptions.MenuGoException(new MenuGoBE.Exceptions.ErrorResult(400, "Món hoặc số lượng không khớp với món thừa đã chọn.", 400)));

            // Act & Assert
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.ApplyLeftoverReuseAsync(200, 999));
            Assert.Equal("Waiting", detail.CookingStatus);
            _detailRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
