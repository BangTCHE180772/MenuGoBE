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
        private readonly OrderDetailService _service;

        public OrderDetailServiceTests()
        {
            _detailRepoMock = new Mock<IOrderDetailRepository>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _mapperMock = new Mock<IMapper>();

            _service = new OrderDetailService(
                _detailRepoMock.Object,
                _orderRepoMock.Object,
                _mapperMock.Object
            );
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
                Image = image
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
    }
}
