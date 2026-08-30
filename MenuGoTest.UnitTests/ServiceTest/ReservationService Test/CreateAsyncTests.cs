using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class CreateAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ThrowsException_WhenReservationTimeTooSoon()
    {
        var dto = new ReservationCreateDto
        {
            CustomerName = "Test", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddMinutes(-5), // < hiện tại
            NumberOfGuests = 4
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        Assert.Contains("sau thời gian hiện tại", ex.Message);
    }

    [Fact]
    public async Task CreatesNewCustomer_WhenPhoneNotExists()
    {
        var dto = new ReservationCreateDto
        {
            CustomerName = "Nguyễn Văn A", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddHours(2),
            NumberOfGuests = 4
        };

        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());

        await _service.CreateAsync(dto);

        _customerRepoMock.Verify(r => r.CreateAsync(It.Is<Customer>(c => c.Phone == "0987654321" && c.Name == "Nguyễn Văn A")), Times.Once);
    }

    [Fact]
    public async Task UpdatesCustomerName_WhenNameChanged()
    {
        var existing = new Customer { Id = 1, Name = "Old Name", Phone = "0987654321" };
        var dto = new ReservationCreateDto
        {
            CustomerName = "New Name", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddHours(2),
            NumberOfGuests = 2,
            ConfirmUpdateCustomer = true // Required since names differ
        };

        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync(existing);
        _customerRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());

        await _service.CreateAsync(dto);

        Assert.Equal("New Name", existing.Name);
    }

    [Fact]
    public async Task ThrowsException_WhenTableAlreadyBooked()
    {
        var dto = new ReservationCreateDto
        {
            CustomerName = "Test", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddHours(2),
            NumberOfGuests = 4,
            TableIds = new List<long> { 1 }
        };

        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync(new Customer { Id = 1, Name = "Test" }); // Name must match to skip conflict

        // Bàn 1 đã bị đặt trong khoảng thời gian trùng
        var overlapping = new List<Reservation>
        {
            new Reservation
            {
                Id = 99, Status = "Pending", OrderId = 50,
                ReservationTime = DateTime.UtcNow.AddHours(2), // match the dto time
                Order = new Order { Id = 50, TableId = 1, Status = "Reserved" }
            }
        };
        _reservationRepoMock.Setup(r => r.GetOverlappingReservationsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(overlapping);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(50)).ReturnsAsync(new List<Order>());
        _reservationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync(new Reservation { Id = 1, ReservationTime = DateTime.UtcNow.AddHours(2) });
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Table { Id = 1, Status = "Occupied", AreaId = 1 });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Không thể xếp bàn", ex.Message);
    }

    [Fact]
    public async Task CreatesOrderWithPreOrderItems()
    {
        var dto = new ReservationCreateDto
        {
            CustomerName = "Test", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddHours(2),
            NumberOfGuests = 4,
            TableIds = new List<long> { 1 },
            PreOrderItems = new List<PreOrderItemDto>
            {
                new PreOrderItemDto { ProductId = 10, Quantity = 2, Note = "Ít cay" }
            }
        };

        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync(new Customer { Id = 1, Name = "Test" }); // Name must match to skip conflict
        _reservationRepoMock.Setup(r => r.GetOverlappingReservationsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<Reservation>());
        _orderRepoMock.Setup(r => r.CreateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Table { Id = 1, Status = "Empty", AreaId = 1 });
        _tableRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Table>())).Returns(Task.CompletedTask);
        _tableRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _productRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Product { Id = 10, Name = "Phở", SellPrice = 50000 });
        _detailRepoMock.Setup(r => r.CreateAsync(It.IsAny<OrderDetail>())).Returns(Task.CompletedTask);
        _detailRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
            .Callback<Reservation>(res => {
                res.Id = 1;
                _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(res);
            })
            .Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());

        await _service.CreateAsync(dto);

        _detailRepoMock.Verify(r => r.CreateAsync(It.Is<OrderDetail>(od => od.ProductId == 10 && od.Quantity == 2 && od.Status == "PreOrder")), Times.Once);
    }

    [Fact]
    public async Task TriggersSignalService_OnCreate()
    {
        var dto = new ReservationCreateDto
        {
            CustomerName = "Test", CustomerPhone = "0987654321",
            ReservationTime = DateTime.UtcNow.AddHours(2),
            NumberOfGuests = 2
        };

        _customerRepoMock.Setup(r => r.GetByPhoneAsync("0987654321")).ReturnsAsync(new Customer { Id = 1, Name = "Test" });
        _reservationRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reservationRepoMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Reservation>());

        await _service.CreateAsync(dto);

        _signalServiceMock.Verify(s => s.TriggerSignal(), Times.Once);
    }
}
