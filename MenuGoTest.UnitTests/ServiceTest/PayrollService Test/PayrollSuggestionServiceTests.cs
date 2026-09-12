using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Repositories;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.PayrollService_Test
{
    public class PayrollSuggestionServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private async Task SeedBasicEntitiesAsync(AppDbContext context)
        {
            var branch = new Branch { Id = 1, Name = "Chi nhánh Hà Nội" };
            var account1 = new Account { Id = 10, Name = "Nhân viên A" };
            var account2 = new Account { Id = 2, Name = "Quản lý B" };

            if (!await context.Branches.AnyAsync(b => b.Id == 1))
                await context.Branches.AddAsync(branch);
            if (!await context.Accounts.AnyAsync(a => a.Id == 10))
                await context.Accounts.AddAsync(account1);
            if (!await context.Accounts.AnyAsync(a => a.Id == 2))
                await context.Accounts.AddAsync(account2);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_ShouldCreatePendingSuggestion()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBasicEntitiesAsync(context);
            var repo = new PayrollSuggestionRepository(context);
            var service = new PayrollSuggestionService(repo, context);

            var dto = new PayrollSuggestionCreateDto
            {
                BranchId = 1,
                AccountId = 10,
                Month = 9,
                Year = 2026,
                Title = "Chấm thiếu 1 ca làm",
                Type = SalaryAdjustmentType.Allowance,
                Amount = 200000,
                Reason = "Quên quẹt thẻ ra",
                ApplyOption = "NextMonth",
                IsResigned = false
            };

            // Act
            var result = await service.CreateAsync(10, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Chấm thiếu 1 ca làm", result.Title);
            Assert.Equal(200000, result.Amount);
            Assert.Equal("Pending", result.Status);
            Assert.False(result.IsApplied);
        }

        [Fact]
        public async Task ProcessAsync_ApproveNextMonth_ShouldTargetNextMonth()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBasicEntitiesAsync(context);
            var repo = new PayrollSuggestionRepository(context);
            var service = new PayrollSuggestionService(repo, context);

            var suggestion = new PayrollSuggestion
            {
                BranchId = 1,
                AccountId = 10,
                Month = 9,
                Year = 2026,
                Title = "Cộng phụ cấp ca đêm",
                Type = SalaryAdjustmentType.Allowance,
                Amount = 150000,
                Reason = "Ca làm đêm chưa tính phụ cấp",
                ApplyOption = "NextMonth",
                IsResigned = false,
                Status = PayrollSuggestionStatus.Pending,
                CreatedBy = 10,
                CreatedAt = DateTime.UtcNow
            };

            await context.PayrollSuggestions.AddAsync(suggestion);
            await context.SaveChangesAsync();

            var processDto = new PayrollSuggestionProcessDto
            {
                Status = "Approved",
                ManagerNote = "Đã xác minh và đồng ý"
            };

            // Act
            var result = await service.ProcessAsync(suggestion.Id, 2, processDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Approved", result.Status);
            Assert.Equal(10, result.Month); // (9 % 12) + 1 = 10
            Assert.Equal(2026, result.Year);
            Assert.Equal("Đã xác minh và đồng ý", result.ManagerNote);
        }

        [Fact]
        public async Task ProcessAsync_ApproveResigningStaff_ShouldTargetCurrentMonth()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            await SeedBasicEntitiesAsync(context);
            var repo = new PayrollSuggestionRepository(context);
            var service = new PayrollSuggestionService(repo, context);

            // Create target payroll for current month
            var payroll = new Payroll
            {
                BranchId = 1,
                AccountId = 10,
                Month = 9,
                Year = 2026,
                CalculatedSalary = 5000000,
                TotalAllowance = 0,
                BonusAmount = 0,
                TotalDeduction = 0,
                PenaltyAmount = 0,
                NetSalary = 5000000,
                Status = PayrollStatus.Draft,
                CreatedBy = 2,
                CreatedAt = DateTime.UtcNow
            };
            await context.Payrolls.AddAsync(payroll);

            var suggestion = new PayrollSuggestion
            {
                BranchId = 1,
                AccountId = 10,
                Month = 9,
                Year = 2026,
                Title = "Thanh toán nốt thưởng nghỉ việc",
                Type = SalaryAdjustmentType.Bonus,
                Amount = 500000,
                Reason = "Nhân viên nghỉ việc từ 15/09",
                ApplyOption = "NextMonth",
                IsResigned = true, // Force current month
                Status = PayrollSuggestionStatus.Pending,
                CreatedBy = 10,
                CreatedAt = DateTime.UtcNow
            };
            await context.PayrollSuggestions.AddAsync(suggestion);
            await context.SaveChangesAsync();

            var processDto = new PayrollSuggestionProcessDto
            {
                Status = "Approved",
                ManagerNote = "Duyệt quyết toán nghỉ việc ngay"
            };

            // Act
            var result = await service.ProcessAsync(suggestion.Id, 2, processDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Approved", result.Status);
            Assert.Equal(9, result.Month); // Remained current month 9 because IsResigned is true
            Assert.True(result.IsApplied);
            Assert.Equal(payroll.Id, result.AppliedPayrollId);

            var updatedPayroll = await context.Payrolls.Include(p => p.SalaryDetails).FirstOrDefaultAsync(p => p.Id == payroll.Id);
            Assert.NotNull(updatedPayroll);
            Assert.Single(updatedPayroll.SalaryDetails);
            Assert.Equal(5500000, updatedPayroll.NetSalary); // 5,000,000 + 500,000
        }
    }
}
