using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class GetAllAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ReturnsMappedList_WhenReservationsExist()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                CustomerId = 10,
                Customer = new Customer { Id = 10, Name = "Nguyễn Văn A", Phone = "0901234567" },
                ReservationTime = DateTime.UtcNow.AddHours(2),
                NumberOfGuests = 4,
                Note = "Gần cửa sổ",
                Status = "Pending",
                OrderId = 100,
                Order = new Order
                {
                    Id = 100,
                    Table = new Table { Id = 1, Name = "Bàn 1" },
                    OrderDetails = new List<OrderDetail>
                    {
                        new OrderDetail { Status = "PreOrder", CookingStatus = "PreOrder" }
                    }
                },
                CreatedAt = DateTime.UtcNow
            }
        };

        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(reservations);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(100)).ReturnsAsync(new List<Order>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(1, dto.Id);
        Assert.Equal("Nguyễn Văn A", dto.CustomerName);
        Assert.Equal("0901234567", dto.CustomerPhone);
        Assert.Single(dto.TableNames);
        Assert.Equal("Bàn 1", dto.TableNames[0]);
        Assert.Equal(1, dto.PreOrderItemsCount);
    }

    [Fact]
    public async Task IncludesChildTableNames_WhenMergedOrdersExist()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 2,
                CustomerId = 11,
                Customer = new Customer { Id = 11, Name = "Trần Thị B", Phone = "0909999888" },
                ReservationTime = DateTime.UtcNow.AddHours(3),
                NumberOfGuests = 8,
                Status = "Pending",
                OrderId = 200,
                Order = new Order
                {
                    Id = 200,
                    Table = new Table { Id = 2, Name = "Bàn 2" }
                }
            }
        };

        var childOrders = new List<Order>
        {
            new Order { Id = 201, Table = new Table { Id = 3, Name = "Bàn 3" } }
        };

        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(reservations);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(200)).ReturnsAsync(childOrders);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].TableNames.Count);
        Assert.Contains("Bàn 2", result[0].TableNames);
        Assert.Contains("Bàn 3", result[0].TableNames);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoReservations()
    {
        // Arrange
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
