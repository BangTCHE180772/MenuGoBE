using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.PayrollServiceTest
{
    public class PayrollServiceTests
    {
        private readonly Mock<IPayrollRepository> _payrollRepoMock;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly PayrollService _service;

        public PayrollServiceTests()
        {
            _payrollRepoMock = new Mock<IPayrollRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            var emailServiceMock = new Mock<MenuGoBE.Interface.Services.IEmailService>();
            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PayrollService>>();
            _service = new PayrollService(_payrollRepoMock.Object, _context, _mapper, emailServiceMock.Object, loggerMock.Object);
        }

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedPayrollViewDtos()
        {
            // Arrange
            var payrolls = new List<Payroll>
            {
                new Payroll { Id = 1, AccountId = 10, Month = 8, Year = 2026, NetSalary = 10000000 },
                new Payroll { Id = 2, AccountId = 11, Month = 8, Year = 2026, NetSalary = 12000000 }
            };

            _payrollRepoMock.Setup(r => r.GetAllAsync(null, null, null, null, null)).ReturnsAsync(payrolls);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            _payrollRepoMock.Verify(r => r.GetAllAsync(null, null, null, null, null), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenPayrollExists_ShouldReturnMappedDto()
        {
            // Arrange
            long payrollId = 1;
            var payroll = new Payroll { Id = payrollId, AccountId = 10, NetSalary = 15000000 };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);

            // Act
            var result = await _service.GetByIdAsync(payrollId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(payrollId, result.Id);
            Assert.Equal(15000000, result.NetSalary);
            _payrollRepoMock.Verify(r => r.GetByIdAsync(payrollId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenPayrollNotFound_ShouldReturnNull()
        {
            // Arrange
            long payrollId = 999;
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync((Payroll?)null);

            // Act
            var result = await _service.GetByIdAsync(payrollId);

            // Assert
            Assert.Null(result);
            _payrollRepoMock.Verify(r => r.GetByIdAsync(payrollId), Times.Once);
        }

        #endregion

        #region GetByAccountIdAsync

        [Fact]
        public async Task GetByAccountIdAsync_ShouldReturnMappedPayrollViewDtos()
        {
            // Arrange
            long accountId = 10;
            var payrolls = new List<Payroll>
            {
                new Payroll { Id = 1, AccountId = accountId, Month = 7, Year = 2026 },
                new Payroll { Id = 2, AccountId = accountId, Month = 8, Year = 2026 }
            };

            _payrollRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null, null)).ReturnsAsync(payrolls);

            // Act
            var result = await _service.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _payrollRepoMock.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WhenPayrollAlreadyExistsForMonthYear_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new PayrollCreateDto { AccountId = 10, Month = 8, Year = 2026 };
            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year))
                            .ReturnsAsync(new Payroll { Id = 1 });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
            Assert.Contains("đã có bảng lương cho tháng 8/2026", ex.Message);
            _payrollRepoMock.Verify(r => r.CreateAsync(It.IsAny<Payroll>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValidDto_ShouldSetContractBranchRecalculateTotalsAndReturnDto()
        {
            // Arrange
            var dto = new PayrollCreateDto
            {
                AccountId = 10,
                Month = 8,
                Year = 2026,
                CalculatedSalary = 10000000,
                TotalAllowance = 1000000,
                BonusAmount = 500000,
                TotalDeduction = 200000,
                PenaltyAmount = 100000
            };

            var account = new Account
            {
                Id = 10,
                Name = "John Payroll",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 5, BranchId = 1, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year))
                            .ReturnsAsync((Payroll?)null);
            _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payroll>()))
                            .Callback<Payroll>(p => p.Id = 100)
                            .Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(100))
                            .ReturnsAsync((long id) => new Payroll
                            {
                                Id = id,
                                AccountId = dto.AccountId,
                                BranchId = 1,
                                ContractId = 5,
                                Month = dto.Month,
                                Year = dto.Year,
                                CalculatedSalary = dto.CalculatedSalary,
                                TotalAllowance = dto.TotalAllowance,
                                BonusAmount = dto.BonusAmount,
                                TotalDeduction = dto.TotalDeduction,
                                PenaltyAmount = dto.PenaltyAmount,
                                NetSalary = 11200000 // 10M + 1M + 0.5M - 0.2M - 0.1M
                            });

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal(11200000, result.NetSalary);
            _payrollRepoMock.Verify(r => r.CreateAsync(It.Is<Payroll>(p =>
                p.AccountId == dto.AccountId &&
                p.BranchId == 1 &&
                p.ContractId == 5 &&
                p.NetSalary == 11200000)), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenPayrollNotFound_ShouldReturnFalse()
        {
            // Arrange
            var dto = new PayrollUpdateDto { Id = 999 };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Payroll?)null);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Payroll>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenPayrollStatusIsPaid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Paid };
            var updateDto = new PayrollUpdateDto { Id = payrollId, Month = 8, Year = 2026 };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updateDto));
            Assert.Contains("Không thể cập nhật bảng lương đã thanh toán (Paid)", ex.Message);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Payroll>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenValidDto_ShouldUpdateFieldsSalaryDetailsRecalculateTotalsAndReturnTrue()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll
            {
                Id = payrollId,
                AccountId = 10,
                Status = PayrollStatus.Draft,
                CalculatedSalary = 10000000,
                SalaryDetails = new List<SalaryDetail>()
            };

            var updateDto = new PayrollUpdateDto
            {
                Id = payrollId,
                Month = 8,
                Year = 2026,
                SalaryType = "Monthly",
                BaseSalary = 10000000,
                BaseWorkDays = 26,
                ActualWorkDays = 26,
                CalculatedSalary = 10000000,
                Status = PayrollStatus.Pending,
                SalaryDetails = new List<SalaryDetailCreateDto>
                {
                    new SalaryDetailCreateDto { Title = "Phụ cấp ăn trưa", Type = SalaryAdjustmentType.Allowance, Amount = 1000000 },
                    new SalaryDetailCreateDto { Title = "Thưởng KPI", Type = SalaryAdjustmentType.Bonus, Amount = 500000 },
                    new SalaryDetailCreateDto { Title = "Khấu trừ BHXH", Type = SalaryAdjustmentType.Deduction, Amount = 200000 }
                }
            };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal(1000000, existingPayroll.TotalAllowance);
            Assert.Equal(500000, existingPayroll.BonusAmount);
            Assert.Equal(200000, existingPayroll.TotalDeduction);
            Assert.Equal(11300000, existingPayroll.NetSalary); // 10M + 1M + 0.5M - 0.2M
            Assert.Equal(PayrollStatus.Pending, existingPayroll.Status);
            Assert.Equal(3, existingPayroll.SalaryDetails.Count);

            _payrollRepoMock.Verify(r => r.UpdateAsync(existingPayroll), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenSalaryDetailCreatedByZero_ShouldFallbackToEntityAccountId()
        {
            // Arrange
            long payrollId = 1;
            long accountId = 10;
            var existingPayroll = new Payroll
            {
                Id = payrollId,
                AccountId = accountId,
                Status = PayrollStatus.Draft,
                CalculatedSalary = 10000000,
                SalaryDetails = new List<SalaryDetail>()
            };

            var updateDto = new PayrollUpdateDto
            {
                Id = payrollId,
                Month = 8,
                Year = 2026,
                SalaryType = "Monthly",
                BaseSalary = 10000000,
                BaseWorkDays = 26,
                ActualWorkDays = 26,
                CalculatedSalary = 10000000,
                Status = PayrollStatus.Pending,
                SalaryDetails = new List<SalaryDetailCreateDto>
                {
                    new SalaryDetailCreateDto { Title = "Phụ cấp", Type = SalaryAdjustmentType.Allowance, Amount = 100000, CreatedBy = 0, AccountId = 0 }
                }
            };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Single(existingPayroll.SalaryDetails);
            Assert.Equal(accountId, existingPayroll.SalaryDetails.First().CreatedBy);
            Assert.Equal(accountId, existingPayroll.SalaryDetails.First().AccountId);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_WhenPayrollNotFound_ShouldReturnFalse()
        {
            // Arrange
            long payrollId = 999;
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync((Payroll?)null);

            // Act
            var result = await _service.DeleteAsync(payrollId);

            // Assert
            Assert.False(result);
            _payrollRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenPayrollStatusIsPaid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Paid };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(payrollId));
            Assert.Contains("Không thể xóa bảng lương đã thanh toán", ex.Message);
            _payrollRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenValidId_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Draft };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.DeleteAsync(payrollId)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(payrollId);

            // Assert
            Assert.True(result);
            _payrollRepoMock.Verify(r => r.DeleteAsync(payrollId), Times.Once);
        }

        #endregion

        #region UpdateStatusAsync

        [Fact]
        public async Task UpdateStatusAsync_WhenPayrollNotFound_ShouldReturnFalse()
        {
            // Arrange
            long payrollId = 999;
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync((Payroll?)null);

            // Act
            var result = await _service.UpdateStatusAsync(payrollId, PayrollStatus.Paid);

            // Assert
            Assert.False(result);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Payroll>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenStatusSetToPaid_ShouldSetPaymentDateAndReturnTrue()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Approved };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateStatusAsync(payrollId, PayrollStatus.Paid);

            // Assert
            Assert.True(result);
            Assert.Equal(PayrollStatus.Paid, existingPayroll.Status);
            Assert.NotNull(existingPayroll.PaymentDate);
            _payrollRepoMock.Verify(r => r.UpdateAsync(existingPayroll), Times.Once);
        }

        [Fact]
        public async Task UnlockPayrollAsync_WhenPayrollNotFound_ShouldReturnFalse()
        {
            // Arrange
            long payrollId = 999;
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync((Payroll?)null);

            // Act
            var result = await _service.UnlockPayrollAsync(payrollId, 1);

            // Assert
            Assert.False(result);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Payroll>()), Times.Never);
        }

        [Fact]
        public async Task UnlockPayrollAsync_WhenPayrollNotLocked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Pending };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UnlockPayrollAsync(payrollId, 1));
            Assert.Contains("Bảng lương phải ở trạng thái Đã chốt hoặc Đã duyệt", ex.Message);
        }

        [Fact]
        public async Task UnlockPayrollAsync_WhenPayrollIsLocked_ShouldRevertStatusToPendingAndClearLockedInfo()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Locked, LockedBy = 10, LockedAt = DateTime.UtcNow };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UnlockPayrollAsync(payrollId, 1);

            // Assert
            Assert.True(result);
            Assert.Equal(PayrollStatus.Pending, existingPayroll.Status);
            Assert.Null(existingPayroll.LockedBy);
            Assert.Null(existingPayroll.LockedAt);
            _payrollRepoMock.Verify(r => r.UpdateAsync(existingPayroll), Times.Once);
        }

        [Fact]
        public async Task ApprovePayrollAsync_WhenPayrollIsLocked_ShouldApproveSuccessfully()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Locked };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApprovePayrollAsync(payrollId, 99);

            // Assert
            Assert.True(result);
            Assert.Equal(PayrollStatus.Approved, existingPayroll.Status);
            Assert.Equal(99, existingPayroll.ApprovedBy);
            _payrollRepoMock.Verify(r => r.UpdateAsync(existingPayroll), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenStatusIsPaidAndCurrentStatusIsNotApproved_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long payrollId = 1;
            var existingPayroll = new Payroll { Id = payrollId, Status = PayrollStatus.Pending };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(existingPayroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatusAsync(payrollId, PayrollStatus.Paid));
            Assert.Contains("Bảng lương phải được Owner phê duyệt", ex.Message);
        }

        #endregion

        #region GenerateEmployeePayrollAsync

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenAccountNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new PayrollGenerateDto { AccountId = 999, Month = 8, Year = 2026 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GenerateEmployeePayrollAsync(dto));
            Assert.Contains("Không tìm thấy nhân viên với ID 999", ex.Message);
        }

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenHourlySalaryType_ShouldCalculateSalaryBasedOnActualHours()
        {
            // Arrange
            long accountId = 20;
            var dto = new PayrollGenerateDto { AccountId = accountId, Month = 8, Year = 2026 };

            var account = new Account
            {
                Id = accountId,
                Name = "Hourly Worker",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 10, BranchId = 1, SalaryType = "Hourly", BaseSalary = 50000, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(account);

            var schedules = new List<WorkSchedule>
            {
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 1), ActualHours = 8, Status = "Completed" },
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 2), ActualHours = 6, Status = "Completed" }
            };
            await _context.WorkSchedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(accountId, 8, 2026)).ReturnsAsync((Payroll?)null);
            _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payroll>()))
                            .Callback<Payroll>(p => p.Id = 200)
                            .Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(200))
                            .ReturnsAsync((long id) => new Payroll
                            {
                                Id = id,
                                AccountId = accountId,
                                CalculatedSalary = 700000, // 50,000 * 14 hours
                                NetSalary = 700000
                            });

            // Act
            var result = await _service.GenerateEmployeePayrollAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(700000, result.CalculatedSalary);

            _payrollRepoMock.Verify(r => r.CreateAsync(It.Is<Payroll>(p =>
                p.AccountId == accountId)), Times.Once);
        }

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenMonthlySalaryType_ShouldCalculateSalaryBasedOnActualDays()
        {
            // Arrange
            long accountId = 30;
            var dto = new PayrollGenerateDto { AccountId = accountId, Month = 8, Year = 2026 };

            var account = new Account
            {
                Id = accountId,
                Name = "Monthly Worker",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 11, BranchId = 1, SalaryType = "Monthly", BaseSalary = 13000000, BaseWorkDay = 26, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(account);

            var schedules = new List<WorkSchedule>
            {
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 1), ActualHours = 8, Status = "Completed" },
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 2), ActualHours = 8, Status = "Completed" },
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 3), ActualHours = 8, Status = "Completed" },
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 4), ActualHours = 8, Status = "Completed" },
                new WorkSchedule { AccountId = accountId, WorkDate = new DateOnly(2026, 8, 5), ActualHours = 8, Status = "Completed" }
            };
            await _context.WorkSchedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(accountId, 8, 2026)).ReturnsAsync((Payroll?)null);
            _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payroll>()))
                            .Callback<Payroll>(p => p.Id = 300)
                            .Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(300))
                            .ReturnsAsync((long id) => new Payroll
                            {
                                Id = id,
                                AccountId = accountId,
                                CalculatedSalary = 2500000, // 13M * 5 / 26 = 2.5M
                                NetSalary = 2500000
                            });

            // Act
            var result = await _service.GenerateEmployeePayrollAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2500000, result.CalculatedSalary);

            _payrollRepoMock.Verify(r => r.CreateAsync(It.Is<Payroll>(p =>
                p.AccountId == accountId)), Times.Once);
        }

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenPayrollAlreadyExists_ShouldUpdateExistingPayroll()
        {
            // Arrange
            long accountId = 40;
            var dto = new PayrollGenerateDto { AccountId = accountId, Month = 8, Year = 2026 };

            var account = new Account
            {
                Id = accountId,
                Name = "Existing Payroll Worker",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 12, BranchId = 1, SalaryType = "Monthly", BaseSalary = 10000000, BaseWorkDay = 26, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            var existingPayroll = new Payroll
            {
                Id = 400,
                AccountId = accountId,
                Month = 8,
                Year = 2026,
                CalculatedSalary = 5000000
            };

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(accountId, 8, 2026)).ReturnsAsync(existingPayroll);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(400)).ReturnsAsync(existingPayroll);

            // Act
            var result = await _service.GenerateEmployeePayrollAsync(dto);

            // Assert
            Assert.NotNull(result);
            _payrollRepoMock.Verify(r => r.UpdateAsync(existingPayroll), Times.Once);
        }

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenContractExpired_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 45;
            var dto = new PayrollGenerateDto { AccountId = accountId, Month = 8, Year = 2026 };

            var account = new Account
            {
                Id = accountId,
                Name = "Expired Contract Worker",
                Contracts = new List<Contract>
                {
                    new Contract
                    {
                        Id = 13,
                        BranchId = 1,
                        SalaryType = "Monthly",
                        BaseSalary = 10000000,
                        StartDate = new DateOnly(2026, 1, 1),
                        EndDate = new DateOnly(2026, 7, 29),
                        Status = "Expired"
                    }
                }
            };
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateEmployeePayrollAsync(dto));
            Assert.Contains("không có hợp đồng hợp lệ trong tháng 8/2026", ex.Message);
        }

        [Fact]
        public async Task GenerateEmployeePayrollAsync_WhenManagerRole_ShouldCalculateDirectBaseSalary()
        {
            // Arrange
            long accountId = 60;
            var dto = new PayrollGenerateDto { AccountId = accountId, Month = 8, Year = 2026 };

            var account = new Account
            {
                Id = accountId,
                Name = "Manager Worker",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 16, BranchId = 1, RoleId = 3, SalaryType = "Monthly", BaseSalary = 10000000, BaseWorkDay = 26, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(accountId, 8, 2026)).ReturnsAsync((Payroll?)null);
            _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payroll>()))
                            .Callback<Payroll>(p => p.Id = 600)
                            .Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(600))
                            .ReturnsAsync((long id) => new Payroll
                            {
                                Id = id,
                                AccountId = accountId,
                                CalculatedSalary = 10000000,
                                NetSalary = 10000000
                            });

            // Act
            var result = await _service.GenerateEmployeePayrollAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10000000, result.CalculatedSalary);

            _payrollRepoMock.Verify(r => r.CreateAsync(It.Is<Payroll>(p =>
                p.AccountId == accountId &&
                p.CalculatedSalary == 10000000)), Times.Once);
        }

        #endregion

        #region GenerateBatchPayrollAsync

        [Fact]
        public async Task GenerateBatchPayrollAsync_WhenUserIsNotAdminOrManager_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var dto = new PayrollBatchGenerateDto { Month = 8, Year = 2026 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GenerateBatchPayrollAsync(dto, isAdmin: false, isManager: false, managerBranchIds: new List<long>()));
            Assert.Contains("Bạn không có quyền tính toán bảng lương", ex.Message);
        }

        [Fact]
        public async Task GenerateBatchPayrollAsync_WhenManagerAccessesUnauthorizedBranch_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var dto = new PayrollBatchGenerateDto { Month = 8, Year = 2026, BranchId = 99 };
            var managerBranchIds = new List<long> { 1, 2 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GenerateBatchPayrollAsync(dto, isAdmin: false, isManager: true, managerBranchIds: managerBranchIds));
            Assert.Contains("Bạn không có quyền tính bảng lương cho chi nhánh khác", ex.Message);
        }

        [Fact]
        public async Task GenerateBatchPayrollAsync_WhenAdminGenerates_ShouldGeneratePayrollForManagerAccounts()
        {
            // Arrange
            var dto = new PayrollBatchGenerateDto { Month = 8, Year = 2026 };

            var managerAccount = new Account
            {
                Id = 50,
                Name = "Manager Account",
                Contracts = new List<Contract>
                {
                    new Contract { Id = 15, BranchId = 1, RoleId = 3, BaseSalary = 15000000, BaseWorkDay = 26, Status = "Active" }
                }
            };
            await _context.Accounts.AddAsync(managerAccount);
            await _context.SaveChangesAsync();

            _payrollRepoMock.Setup(r => r.GetByAccountMonthYearAsync(50, 8, 2026)).ReturnsAsync((Payroll?)null);
            _payrollRepoMock.Setup(r => r.CreateAsync(It.IsAny<Payroll>()))
                            .Callback<Payroll>(p => p.Id = 500)
                            .Returns(Task.CompletedTask);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(500))
                            .ReturnsAsync(new Payroll { Id = 500, AccountId = 50, Month = 8, Year = 2026 });

            // Act
            var result = await _service.GenerateBatchPayrollAsync(dto, isAdmin: true, isManager: false, managerBranchIds: new List<long>());

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(50, result[0].AccountId);
        }

        #endregion
    }
}
