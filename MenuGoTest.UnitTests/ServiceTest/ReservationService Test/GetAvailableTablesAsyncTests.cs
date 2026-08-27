using MenuGoBE.Models;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public class GetAvailableTablesAsyncTests : ReservationServiceTestBase
{
    [Fact]
    public async Task ReturnsOnlyAvailableTables_ExcludingOverlappingReservations()
    {
        // Arrange
        long branchId = 1;
        var resTime = DateTime.UtcNow.AddHours(2);

        var allTables = new List<Table>
        {
            new Table { Id = 1, Name = "Bàn 1", Status = "Empty", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } },
            new Table { Id = 2, Name = "Bàn 2", Status = "Empty", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } },
            new Table { Id = 3, Name = "Bàn 3", Status = "Maintenance", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } } // Non-empty, should be excluded
        };

        var overlapping = new List<Reservation>
        {
            new Reservation
            {
                Id = 10,
                OrderId = 100,
                Order = new Order { Id = 100, TableId = 1 } // Bàn 1 booked
            }
        };

        _tableRepoMock.Setup(r => r.GetByBranchIdsAsync(It.IsAny<List<long>>())).ReturnsAsync(allTables);
        _reservationRepoMock.Setup(r => r.GetOverlappingReservationsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(overlapping);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(100)).ReturnsAsync(new List<Order>());

        // Act
        var result = await _service.GetAvailableTablesAsync(branchId, resTime);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id); // Only Bàn 2 is available
    }

    [Fact]
    public async Task ExcludesChildTables_InOverlappingReservations()
    {
        // Arrange
        long branchId = 1;
        var resTime = DateTime.UtcNow.AddHours(2);

        var allTables = new List<Table>
        {
            new Table { Id = 1, Name = "Bàn 1", Status = "Empty", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } },
            new Table { Id = 2, Name = "Bàn 2", Status = "Empty", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } },
            new Table { Id = 3, Name = "Bàn 3", Status = "Empty", AreaId = 1, Area = new Area { Id = 1, BranchId = branchId } }
        };

        var overlapping = new List<Reservation>
        {
            new Reservation
            {
                Id = 10,
                OrderId = 100,
                Order = new Order { Id = 100, TableId = 1 } // Father order on Table 1
            }
        };

        var childOrders = new List<Order>
        {
            new Order { Id = 101, TableId = 2 } // Merged child order on Table 2
        };

        _tableRepoMock.Setup(r => r.GetByBranchIdsAsync(It.IsAny<List<long>>())).ReturnsAsync(allTables);
        _reservationRepoMock.Setup(r => r.GetOverlappingReservationsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(overlapping);
        _orderRepoMock.Setup(r => r.GetChildOrdersWithDetailsAsync(100)).ReturnsAsync(childOrders);

        // Act
        var result = await _service.GetAvailableTablesAsync(branchId, resTime);

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].Id); // Only Bàn 3 is available
    }
}
