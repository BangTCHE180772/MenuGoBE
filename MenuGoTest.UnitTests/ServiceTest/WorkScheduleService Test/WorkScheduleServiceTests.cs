using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.WorkScheduleServiceTest
{
    public class WorkScheduleServiceTests
    {
        private readonly Mock<IWorkScheduleRepository> _repoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IShiftRepository> _shiftRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly IMapper _mapper;
        private readonly WorkScheduleService _service;

        public WorkScheduleServiceTests()
        {
            _repoMock = new Mock<IWorkScheduleRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _shiftRepoMock = new Mock<IShiftRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MenuGoBE.Hubs.NotificationHub>>();
            var hubClientsMock = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
            var hubClientProxyMock = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(hubClientProxyMock.Object);
            hubClientsMock.Setup(c => c.All).Returns(hubClientProxyMock.Object);
            hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);
            var notificationRepoMock = new Mock<INotificationRepository>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);

            _service = new WorkScheduleService(
                _repoMock.Object,
                _mapper,
                _branchRepoMock.Object,
                _shiftRepoMock.Object,
                _accountRepoMock.Object,
                hubContextMock.Object,
                context,
                notificationRepoMock.Object);
        }

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedWorkScheduleViewDtos()
        {
            // Arrange
            var schedules = new List<WorkSchedule>
            {
                new WorkSchedule { Id = 1, AccountId = 10, BranchId = 1, ShiftId = 1, WorkDate = new DateOnly(2026, 8, 1), Code = "WS-1" },
                new WorkSchedule { Id = 2, AccountId = 11, BranchId = 1, ShiftId = 2, WorkDate = new DateOnly(2026, 8, 1), Code = "WS-2" }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(schedules);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("WS-1", result[0].Code);
            Assert.Equal("WS-2", result[1].Code);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenScheduleExists_ShouldReturnMappedDto()
        {
            // Arrange
            long scheduleId = 1;
            var schedule = new WorkSchedule { Id = scheduleId, AccountId = 10, Code = "WS-10" };

            _repoMock.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);

            // Act
            var result = await _service.GetByIdAsync(scheduleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(scheduleId, result.Id);
            Assert.Equal("WS-10", result.Code);
            _repoMock.Verify(r => r.GetByIdAsync(scheduleId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenScheduleNotFound_ShouldReturnNull()
        {
            // Arrange
            long scheduleId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync((WorkSchedule?)null);

            // Act
            var result = await _service.GetByIdAsync(scheduleId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(scheduleId), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(null!));
        }

        [Fact]
        public async Task CreateAsync_WhenBranchDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto { BranchId = 99, ShiftId = 1, AccountId = 10 };
            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync((Branch?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("Branch with ID 99 does not exist", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenShiftDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto { BranchId = 1, ShiftId = 99, AccountId = 10 };
            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync((Shift?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("Shift with ID 99 does not exist", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenAccountDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto { BranchId = 1, ShiftId = 1, AccountId = 99 };
            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(new Shift { Id = 1 });
            _accountRepoMock.Setup(r => r.GetByIdAsync(dto.AccountId)).ReturnsAsync((Account?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("Tài khoản với ID 99 không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenAccountHasNoActiveContractAtBranch_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto { BranchId = 1, ShiftId = 1, AccountId = 10 };
            var account = new Account
            {
                Id = 10,
                Name = "John Staff",
                Contracts = new List<Contract>
                {
                    new Contract { BranchId = 2, Status = "Active" },
                    new Contract { BranchId = 1, Status = "Expired" }
                }
            };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(new Shift { Id = 1 });
            _accountRepoMock.Setup(r => r.GetByIdAsync(dto.AccountId)).ReturnsAsync(account);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("chưa có hợp đồng làm việc còn hiệu lực tại chi nhánh này", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenScheduleAlreadyExists_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto
            {
                BranchId = 1,
                ShiftId = 1,
                AccountId = 10,
                WorkDate = new DateOnly(2026, 8, 10)
            };

            var account = new Account
            {
                Id = 10,
                Name = "John Staff",
                Contracts = new List<Contract> { new Contract { BranchId = 1, Status = "Active" } }
            };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(new Shift { Id = 1 });
            _accountRepoMock.Setup(r => r.GetByIdAsync(dto.AccountId)).ReturnsAsync(account);
            _repoMock.Setup(r => r.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("đã được phân công ca làm việc này vào ngày", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenScheduleOverlaps_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto
            {
                BranchId = 1,
                ShiftId = 2,
                AccountId = 10,
                WorkDate = new DateOnly(2026, 8, 10)
            };

            var account = new Account
            {
                Id = 10,
                Name = "John Staff",
                Contracts = new List<Contract> { new Contract { BranchId = 1, Status = "Active" } }
            };

            var shift = new Shift { Id = 2, StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(16, 0) };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(shift);
            _accountRepoMock.Setup(r => r.GetByIdAsync(dto.AccountId)).ReturnsAsync(account);
            _repoMock.Setup(r => r.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate)).ReturnsAsync(false);
            _repoMock.Setup(r => r.HasOverlappingScheduleAsync(dto.AccountId, dto.WorkDate, shift.StartTime, shift.EndTime)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("đã có ca làm việc trùng giờ trong ngày", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenValidDto_ShouldCreateSetCodePendingStatusAndReturnDto()
        {
            // Arrange
            var dto = new WorkScheduleCreateDto
            {
                BranchId = 1,
                ShiftId = 1,
                AccountId = 10,
                WorkDate = new DateOnly(2026, 8, 10)
            };

            var account = new Account
            {
                Id = 10,
                Name = "John Staff",
                Contracts = new List<Contract> { new Contract { BranchId = 1, Status = "Active" } }
            };

            var shift = new Shift { Id = 1, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(shift);
            _accountRepoMock.Setup(r => r.GetByIdAsync(dto.AccountId)).ReturnsAsync(account);
            _repoMock.Setup(r => r.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate)).ReturnsAsync(false);
            _repoMock.Setup(r => r.HasOverlappingScheduleAsync(dto.AccountId, dto.WorkDate, shift.StartTime, shift.EndTime)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.AccountId, result.AccountId);
            Assert.Equal(dto.BranchId, result.BranchId);

            _repoMock.Verify(r => r.CreateAsync(It.Is<WorkSchedule>(ws =>
                ws.AccountId == dto.AccountId &&
                ws.Status == "Pending" &&
                ws.Code == "WS-1-1-10-20260810")), Times.Once);

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region AssignBulkAsync

        [Fact]
        public async Task AssignBulkAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AssignBulkAsync(null!));
        }

        [Fact]
        public async Task AssignBulkAsync_WhenBranchDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleAssignDto { BranchId = 99, ShiftId = 1, AccountIds = new List<long> { 10 } };
            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync((Branch?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignBulkAsync(dto));
            Assert.Contains("Branch with ID 99 does not exist", ex.Message);
        }

        [Fact]
        public async Task AssignBulkAsync_WhenShiftDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleAssignDto { BranchId = 1, ShiftId = 99, AccountIds = new List<long> { 10 } };
            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync((Shift?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignBulkAsync(dto));
            Assert.Contains("Shift with ID 99 does not exist", ex.Message);
        }

        [Fact]
        public async Task AssignBulkAsync_WhenNoAccountsHaveValidContracts_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleAssignDto
            {
                BranchId = 1,
                ShiftId = 1,
                AccountIds = new List<long> { 10, 11 }
            };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(new Shift { Id = 1 });
            _accountRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Account { Id = 10, Contracts = new List<Contract>() });
            _accountRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Account { Id = 11, Contracts = new List<Contract> { new Contract { BranchId = 2, Status = "Active" } } });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AssignBulkAsync(dto));
            Assert.Contains("Không có nhân viên nào trong danh sách được chọn có hợp đồng còn hiệu lực", ex.Message);
        }

        [Fact]
        public async Task AssignBulkAsync_WhenValidAccountsAndDates_ShouldCreateApprovedSchedulesAndReturnList()
        {
            // Arrange
            var dto = new WorkScheduleAssignDto
            {
                BranchId = 1,
                ShiftId = 1,
                AccountIds = new List<long> { 10 },
                WorkDates = new List<DateOnly> { new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 16) },
                CreatedBy = 1
            };

            var account10 = new Account
            {
                Id = 10,
                Contracts = new List<Contract> { new Contract { BranchId = 1, Status = "Active" } }
            };

            var shift = new Shift { Id = 1, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0) };

            _branchRepoMock.Setup(r => r.GetByIdAsync(dto.BranchId)).ReturnsAsync(new Branch { Id = 1 });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(dto.ShiftId)).ReturnsAsync(shift);
            _accountRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(account10);
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateOnly>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.HasOverlappingScheduleAsync(It.IsAny<long>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.AssignBulkAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _repoMock.Verify(r => r.CreateAsync(It.Is<WorkSchedule>(ws => ws.Status == "Approved" && ws.ManagerApproved == true)), Times.Exactly(2));
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenScheduleNotFound_ShouldReturnFalse()
        {
            // Arrange
            var dto = new WorkScheduleUpdateDto { Id = 999 };
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((WorkSchedule?)null);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WorkSchedule>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenAccountHasNoActiveContract_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new WorkScheduleUpdateDto { Id = 1, AccountId = 10, BranchId = 1 };
            var existingSchedule = new WorkSchedule { Id = 1, AccountId = 10, BranchId = 1 };
            var account = new Account { Id = 10, Contracts = new List<Contract>() };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingSchedule);
            _accountRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(account);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(dto));
            Assert.Contains("Nhân viên này chưa có hợp đồng làm việc còn hiệu lực", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_WhenValidDto_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var dto = new WorkScheduleUpdateDto { Id = 1, AccountId = 10, BranchId = 1, Status = "COMPLETED" };
            var existingSchedule = new WorkSchedule { Id = 1, AccountId = 10, BranchId = 1, Status = "WORKING" };
            var account = new Account
            {
                Id = 10,
                Contracts = new List<Contract> { new Contract { BranchId = 1, Status = "Active" } }
            };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingSchedule);
            _accountRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(account);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.UpdateAsync(existingSchedule), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_WhenScheduleExists_ShouldDeleteAndNotifyEmployee()
        {
            // Arrange
            long scheduleId = 1;
            long cancelledBy = 5;
            var schedule = new WorkSchedule { Id = scheduleId, AccountId = 10, BranchId = 1, ShiftId = 2, WorkDate = new DateOnly(2026, 9, 8) };
            _repoMock.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync(schedule);
            _repoMock.Setup(r => r.DeleteAsync(scheduleId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _shiftRepoMock.Setup(r => r.GetByIdAsync(schedule.ShiftId)).ReturnsAsync(new Shift { Id = 2, Name = "Ca sáng" });
            _accountRepoMock.Setup(r => r.GetByIdAsync(cancelledBy)).ReturnsAsync(new Account { Id = cancelledBy, Name = "Quản lý A" });

            // Act
            var result = await _service.DeleteAsync(scheduleId, "Nhân viên xin nghỉ đột xuất", cancelledBy);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(scheduleId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenScheduleNotFound_ShouldReturnFalse()
        {
            // Arrange
            long scheduleId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(scheduleId)).ReturnsAsync((WorkSchedule?)null);

            // Act
            var result = await _service.DeleteAsync(scheduleId, "Lý do bất kỳ", 5);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task DeleteAsync_WhenReasonMissing_ShouldThrowAndNotDelete(string reason)
        {
            // Arrange
            long scheduleId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(scheduleId, reason, 5));
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        #endregion

        #region GetByAccountIdAsync

        [Fact]
        public async Task GetByAccountIdAsync_ShouldReturnMappedWorkScheduleViewDtos()
        {
            // Arrange
            long accountId = 10;
            var schedules = new List<WorkSchedule>
            {
                new WorkSchedule { Id = 1, AccountId = accountId, Code = "WS-1" },
                new WorkSchedule { Id = 2, AccountId = accountId, Code = "WS-2" }
            };

            _repoMock.Setup(r => r.GetByAccountIdAsync(accountId)).ReturnsAsync(schedules);

            // Act
            var result = await _service.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _repoMock.Verify(r => r.GetByAccountIdAsync(accountId), Times.Once);
        }

        #endregion

        #region RecordAttendanceAsync

        [Fact]
        public async Task RecordAttendanceAsync_WhenNoSchedulesToday_ShouldReturnFailureResult()
        {
            // Arrange
            long accountId = 10;
            long branchId = 1;

            _repoMock.Setup(r => r.GetTodaySchedulesAsync(accountId, branchId, It.IsAny<DateOnly>()))
                     .ReturnsAsync(new List<WorkSchedule>());

            // Act
            var result = await _service.RecordAttendanceAsync(accountId, branchId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Bạn không có ca làm việc hôm nay", result.Message);
        }

        [Fact]
        public async Task RecordAttendanceAsync_WhenWorkingScheduleExists_ShouldCheckOutAndReturnSuccess()
        {
            // Arrange
            long accountId = 10;
            long branchId = 1;
            var now = DateTime.UtcNow;

            var workingSchedule = new WorkSchedule
            {
                Id = 1,
                AccountId = accountId,
                BranchId = branchId,
                Status = "Working",
                CheckInAt = now.AddHours(-4),
                Shift = new Shift { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(18, 0) }
            };

            _repoMock.Setup(r => r.GetTodaySchedulesAsync(accountId, branchId, It.IsAny<DateOnly>()))
                     .ReturnsAsync(new List<WorkSchedule> { workingSchedule });
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RecordAttendanceAsync(accountId, branchId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("checkout", result.Action);
            Assert.Equal("Completed", workingSchedule.Status);
            Assert.NotNull(workingSchedule.CheckOutAt);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RecordAttendanceAsync_WhenNoWorkingSchedule_ShouldCheckInFirstAvailableShift()
        {
            // Arrange
            long accountId = 10;
            long branchId = 1;

            var scheduledShift = new WorkSchedule
            {
                Id = 1,
                AccountId = accountId,
                BranchId = branchId,
                Status = "Scheduled",
                CheckInAt = null,
                Shift = new Shift { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) }
            };

            _repoMock.Setup(r => r.GetTodaySchedulesAsync(accountId, branchId, It.IsAny<DateOnly>()))
                     .ReturnsAsync(new List<WorkSchedule> { scheduledShift });
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkSchedule>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RecordAttendanceAsync(accountId, branchId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("checkin", result.Action);
            Assert.Equal("Working", scheduledShift.Status);
            Assert.NotNull(scheduledShift.CheckInAt);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetActiveRoleIdsAsync

        [Fact]
        public async Task GetActiveRoleIdsAsync_ShouldReturnRoleIdsFromAccountRepo()
        {
            // Arrange
            long accountId = 10;
            var expectedRoles = new List<long> { 4, 6 };
            _accountRepoMock.Setup(r => r.GetActiveRoleIdsAsync(accountId)).ReturnsAsync(expectedRoles);

            // Act
            var result = await _service.GetActiveRoleIdsAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(4, result[0]);
            Assert.Equal(6, result[1]);
            _accountRepoMock.Verify(r => r.GetActiveRoleIdsAsync(accountId), Times.Once);
        }

        #endregion
    }
}
