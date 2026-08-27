using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ShiftChangeRequestServiceTest;

public class ShiftChangeRequestServiceTests
{
    private readonly Mock<IShiftChangeRequestRepository> _repoMock;
    private readonly Mock<IWorkScheduleRepository> _workScheduleRepoMock;
    private readonly Mock<IShiftRepository> _shiftRepoMock;
    private readonly ShiftChangeRequestService _service;

    public ShiftChangeRequestServiceTests()
    {
        _repoMock = new Mock<IShiftChangeRequestRepository>();
        _workScheduleRepoMock = new Mock<IWorkScheduleRepository>();
        _shiftRepoMock = new Mock<IShiftRepository>();
        _service = new ShiftChangeRequestService(
            _repoMock.Object,
            _workScheduleRepoMock.Object,
            _shiftRepoMock.Object,
            new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>().Object);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ShiftChangeRequest>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((ShiftChangeRequest?)null);
        _workScheduleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
        _workScheduleRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    private static WorkSchedule FutureSchedule(long id = 1, long accountId = 10, long branchId = 5, long shiftId = 100, string status = "")
    {
        return new WorkSchedule
        {
            Id = id,
            AccountId = accountId,
            BranchId = branchId,
            ShiftId = shiftId,
            WorkDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).AddDays(1),
            Status = status,
            ManagerApproved = false,
        };
    }

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_Throws_WhenScheduleNotFound()
    {
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((WorkSchedule?)null);
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewShiftId = 2 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(10, dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenScheduleBelongsToAnotherAccount()
    {
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FutureSchedule(accountId: 99));
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewShiftId = 2 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(10, dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNoNewShiftOrCustomTimeGiven()
    {
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FutureSchedule());
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true });
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(10, dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNewShiftSameAsOld()
    {
        var oldShift = new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FutureSchedule());
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(oldShift);
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewShiftId = 100 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(10, dto));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenOverlappingScheduleExists()
    {
        var oldShift = new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        var newShift = new Shift { Id = 200, Name = "Ca chiều", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), IsActive = true };
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FutureSchedule());
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(oldShift);
        _shiftRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(newShift);
        _workScheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(10, It.IsAny<DateOnly>(), newShift.StartTime, newShift.EndTime)).ReturnsAsync(true);
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewShiftId = 200 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(10, dto));
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WithTemplateShift_AndMarksSchedulePending()
    {
        var oldShift = new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        var newShift = new Shift { Id = 200, Name = "Ca chiều", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), IsActive = true };
        var schedule = FutureSchedule();
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(oldShift);
        _shiftRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(newShift);
        _workScheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(10, It.IsAny<DateOnly>(), newShift.StartTime, newShift.EndTime)).ReturnsAsync(false);
        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewShiftId = 200, Reason = "Bận việc gia đình" };

        var result = await _service.CreateAsync(10, dto);

        Assert.Equal(ShiftChangeRequestStatus.Pending, result.Status);
        Assert.Equal(200, result.NewShiftId);
        Assert.Equal("SHIFT_CHANGE_PENDING", schedule.Status);
        Assert.False(schedule.ManagerApproved);
        _repoMock.Verify(r => r.CreateAsync(It.Is<ShiftChangeRequest>(req =>
            req.OldShiftId == 100 && req.NewShiftId == 200 && req.AccountId == 10)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CreatesCustomShift_AndFlagsNightShift_WhenCustomHoursCrossMidnight()
    {
        var oldShift = new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        var schedule = FutureSchedule();
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(oldShift);
        _shiftRepoMock.Setup(r => r.GetByTimeAsync(new TimeOnly(22, 0), new TimeOnly(6, 0))).ReturnsAsync((Shift?)null);
        _shiftRepoMock.Setup(r => r.CreateAsync(It.IsAny<Shift>())).Returns(Task.CompletedTask);
        _shiftRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _workScheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(10, It.IsAny<DateOnly>(), new TimeOnly(22, 0), new TimeOnly(6, 0))).ReturnsAsync(false);

        var dto = new ShiftChangeRequestCreateDto
        {
            WorkScheduleId = 1,
            NewStartTime = new TimeOnly(22, 0),
            NewEndTime = new TimeOnly(6, 0),
        };

        var result = await _service.CreateAsync(10, dto);

        Assert.True(result.IsNightShift);
        _shiftRepoMock.Verify(r => r.CreateAsync(It.Is<Shift>(s => s.StartTime == new TimeOnly(22, 0) && s.EndTime == new TimeOnly(6, 0))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReusesExistingShift_WhenCustomTimeMatchesAnExistingTemplate()
    {
        var oldShift = new Shift { Id = 100, Name = "Ca sáng", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), IsActive = true };
        var matchingShift = new Shift { Id = 300, Name = "Ca chiều tối", StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(23, 0), IsActive = true };
        var schedule = FutureSchedule();
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _shiftRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(oldShift);
        _shiftRepoMock.Setup(r => r.GetByTimeAsync(new TimeOnly(16, 0), new TimeOnly(23, 0))).ReturnsAsync(matchingShift);
        _workScheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(10, It.IsAny<DateOnly>(), matchingShift.StartTime, matchingShift.EndTime)).ReturnsAsync(false);

        var dto = new ShiftChangeRequestCreateDto { WorkScheduleId = 1, NewStartTime = new TimeOnly(16, 0), NewEndTime = new TimeOnly(23, 0) };

        var result = await _service.CreateAsync(10, dto);

        Assert.Equal(300, result.NewShiftId);
        _shiftRepoMock.Verify(r => r.CreateAsync(It.IsAny<Shift>()), Times.Never);
    }

    #endregion

    #region ApproveAsync / RejectAsync

    [Fact]
    public async Task ApproveAsync_ReturnsFalse_WhenRequestNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ShiftChangeRequest?)null);

        var result = await _service.ApproveAsync(1, 99);

        Assert.False(result);
    }

    [Fact]
    public async Task ApproveAsync_UpdatesScheduleShiftAndMarksApproved()
    {
        var schedule = FutureSchedule(status: "SHIFT_CHANGE_PENDING");
        var request = new ShiftChangeRequest
        {
            Id = 1,
            WorkScheduleId = 1,
            AccountId = 10,
            BranchId = 5,
            NewShiftId = 200,
            Status = ShiftChangeRequestStatus.Pending,
        };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);

        var result = await _service.ApproveAsync(1, 99);

        Assert.True(result);
        Assert.Equal(200, schedule.ShiftId);
        Assert.True(schedule.ManagerApproved);
        Assert.Equal(ShiftChangeRequestStatus.Approved, request.Status);
        Assert.Equal(99, request.ApprovedBy);
    }

    [Fact]
    public async Task ApproveAsync_ReturnsFalse_WhenAlreadyProcessed()
    {
        var request = new ShiftChangeRequest { Id = 1, Status = ShiftChangeRequestStatus.Approved };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        var result = await _service.ApproveAsync(1, 99);

        Assert.False(result);
    }

    [Fact]
    public async Task RejectAsync_RestoresOldScheduleStatus()
    {
        var schedule = FutureSchedule(status: "SHIFT_CHANGE_PENDING");
        var request = new ShiftChangeRequest
        {
            Id = 1,
            WorkScheduleId = 1,
            OldStatus = "Approved",
            OldManagerApproved = true,
            Status = ShiftChangeRequestStatus.Pending,
        };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);

        var result = await _service.RejectAsync(1, 99, "Không đủ nhân sự");

        Assert.True(result);
        Assert.Equal("Approved", schedule.Status);
        Assert.True(schedule.ManagerApproved);
        Assert.Equal(ShiftChangeRequestStatus.Rejected, request.Status);
        Assert.Equal("Không đủ nhân sự", request.RejectReason);
    }

    #endregion
}
