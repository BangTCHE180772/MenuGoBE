using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.SalaryDetailServiceTest
{
    public class SalaryDetailServiceTests
    {
        private readonly Mock<ISalaryDetailRepository> _salaryDetailRepoMock;
        private readonly Mock<IPayrollRepository> _payrollRepoMock;
        private readonly IMapper _mapper;
        private readonly SalaryDetailService _service;

        public SalaryDetailServiceTests()
        {
            _salaryDetailRepoMock = new Mock<ISalaryDetailRepository>();
            _payrollRepoMock = new Mock<IPayrollRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new SalaryDetailService(_salaryDetailRepoMock.Object, _payrollRepoMock.Object, _mapper);
        }

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedSalaryDetailViewDtos()
        {
            // Arrange
            var details = new List<SalaryDetail>
            {
                new SalaryDetail { Id = 1, PayrollId = 10, Title = "Phụ cấp xăng xe", Type = SalaryAdjustmentType.Allowance, Amount = 500000 },
                new SalaryDetail { Id = 2, PayrollId = 10, Title = "Thưởng KPI", Type = SalaryAdjustmentType.Bonus, Amount = 1000000 }
            };

            _salaryDetailRepoMock.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(details);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Phụ cấp xăng xe", result[0].Title);
            Assert.Equal("Thưởng KPI", result[1].Title);
            _salaryDetailRepoMock.Verify(r => r.GetAllAsync(null, null, null, null), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenDetailExists_ShouldReturnMappedDto()
        {
            // Arrange
            long detailId = 1;
            var detail = new SalaryDetail { Id = detailId, PayrollId = 10, Title = "Phụ cấp ăn trưa", Amount = 1000000 };

            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync(detail);

            // Act
            var result = await _service.GetByIdAsync(detailId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(detailId, result.Id);
            Assert.Equal("Phụ cấp ăn trưa", result.Title);
            _salaryDetailRepoMock.Verify(r => r.GetByIdAsync(detailId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenDetailNotFound_ShouldReturnNull()
        {
            // Arrange
            long detailId = 999;
            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync((SalaryDetail?)null);

            // Act
            var result = await _service.GetByIdAsync(detailId);

            // Assert
            Assert.Null(result);
            _salaryDetailRepoMock.Verify(r => r.GetByIdAsync(detailId), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WhenPayrollNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new SalaryDetailCreateDto { PayrollId = 999, Title = "Phụ cấp", Amount = 500000 };
            _payrollRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Payroll?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(dto));
            Assert.Contains("Không tìm thấy bảng lương với ID 999", ex.Message);
            _salaryDetailRepoMock.Verify(r => r.CreateAsync(It.IsAny<SalaryDetail>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPayrollStatusIsPaid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var payroll = new Payroll { Id = 10, Status = PayrollStatus.Paid };
            var dto = new SalaryDetailCreateDto { PayrollId = 10, Title = "Phụ cấp", Amount = 500000 };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(payroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
            Assert.Contains("Không thể thêm chi tiết lương vào bảng lương đã thanh toán (Paid)", ex.Message);
            _salaryDetailRepoMock.Verify(r => r.CreateAsync(It.IsAny<SalaryDetail>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValidDto_ShouldCreateDetailAndRecalculatePayrollTotals()
        {
            // Arrange
            long payrollId = 10;
            var payroll = new Payroll
            {
                Id = payrollId,
                AccountId = 5,
                Status = PayrollStatus.Draft,
                CalculatedSalary = 10000000
            };

            var dto = new SalaryDetailCreateDto
            {
                PayrollId = payrollId,
                Title = "Phụ cấp ăn trưa",
                Type = SalaryAdjustmentType.Allowance,
                Amount = 1000000
            };

            var newDetail = new SalaryDetail
            {
                Id = 100,
                PayrollId = payrollId,
                AccountId = 5,
                Title = dto.Title,
                Type = dto.Type,
                Amount = dto.Amount
            };

            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);
            _salaryDetailRepoMock.Setup(r => r.CreateAsync(It.IsAny<SalaryDetail>()))
                                 .Callback<SalaryDetail>(d => d.Id = 100)
                                 .Returns(Task.CompletedTask);
            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(newDetail);

            // Mock recalculate: GetAllAsync for this payroll
            var allDetails = new List<SalaryDetail> { newDetail };
            _salaryDetailRepoMock.Setup(r => r.GetAllAsync(payrollId, null, null, null)).ReturnsAsync(allDetails);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal("Phụ cấp ăn trưa", result.Title);

            _salaryDetailRepoMock.Verify(r => r.CreateAsync(It.Is<SalaryDetail>(d =>
                d.PayrollId == payrollId &&
                d.AccountId == 5 &&
                d.Amount == 1000000)), Times.Once);

            // Verify payroll total recalculation
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.Is<Payroll>(p =>
                p.Id == payrollId &&
                p.TotalAllowance == 1000000 &&
                p.NetSalary == 11000000)), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenDetailNotFound_ShouldReturnFalse()
        {
            // Arrange
            var dto = new SalaryDetailUpdateDto { Id = 999 };
            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((SalaryDetail?)null);

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result);
            _salaryDetailRepoMock.Verify(r => r.UpdateAsync(It.IsAny<SalaryDetail>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenPayrollStatusIsPaid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long detailId = 1;
            long payrollId = 10;
            var existingDetail = new SalaryDetail { Id = detailId, PayrollId = payrollId };
            var payroll = new Payroll { Id = payrollId, Status = PayrollStatus.Paid };
            var updateDto = new SalaryDetailUpdateDto { Id = detailId, Title = "Updated", Amount = 2000000 };

            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync(existingDetail);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updateDto));
            Assert.Contains("Không thể cập nhật chi tiết lương của bảng lương đã thanh toán (Paid)", ex.Message);
            _salaryDetailRepoMock.Verify(r => r.UpdateAsync(It.IsAny<SalaryDetail>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenValidDto_ShouldUpdateFieldsAndRecalculatePayrollTotals()
        {
            // Arrange
            long detailId = 1;
            long payrollId = 10;
            var existingDetail = new SalaryDetail
            {
                Id = detailId,
                PayrollId = payrollId,
                Title = "Phụ cấp cũ",
                Type = SalaryAdjustmentType.Allowance,
                Amount = 500000
            };

            var payroll = new Payroll
            {
                Id = payrollId,
                Status = PayrollStatus.Draft,
                CalculatedSalary = 10000000
            };

            var updateDto = new SalaryDetailUpdateDto
            {
                Id = detailId,
                Title = "Phụ cấp mới",
                Type = SalaryAdjustmentType.Bonus,
                Amount = 1500000,
                Note = "Ghi chú mới"
            };

            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync(existingDetail);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);
            _salaryDetailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SalaryDetail>())).Returns(Task.CompletedTask);

            // Mock recalculate: updated detail list
            var updatedDetailsList = new List<SalaryDetail>
            {
                new SalaryDetail { Id = detailId, PayrollId = payrollId, Type = SalaryAdjustmentType.Bonus, Amount = 1500000 }
            };
            _salaryDetailRepoMock.Setup(r => r.GetAllAsync(payrollId, null, null, null)).ReturnsAsync(updatedDetailsList);
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Phụ cấp mới", existingDetail.Title);
            Assert.Equal(SalaryAdjustmentType.Bonus, existingDetail.Type);
            Assert.Equal(1500000, existingDetail.Amount);

            _salaryDetailRepoMock.Verify(r => r.UpdateAsync(existingDetail), Times.Once);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.Is<Payroll>(p =>
                p.Id == payrollId &&
                p.BonusAmount == 1500000 &&
                p.TotalAllowance == 0 &&
                p.NetSalary == 11500000)), Times.Once);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_WhenDetailNotFound_ShouldReturnFalse()
        {
            // Arrange
            long detailId = 999;
            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync((SalaryDetail?)null);

            // Act
            var result = await _service.DeleteAsync(detailId);

            // Assert
            Assert.False(result);
            _salaryDetailRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenPayrollStatusIsPaid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long detailId = 1;
            long payrollId = 10;
            var existingDetail = new SalaryDetail { Id = detailId, PayrollId = payrollId };
            var payroll = new Payroll { Id = payrollId, Status = PayrollStatus.Paid };

            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync(existingDetail);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(detailId));
            Assert.Contains("Không thể xóa chi tiết lương của bảng lương đã thanh toán (Paid)", ex.Message);
            _salaryDetailRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenValidId_ShouldDeleteDetailAndRecalculatePayrollTotals()
        {
            // Arrange
            long detailId = 1;
            long payrollId = 10;
            var existingDetail = new SalaryDetail { Id = detailId, PayrollId = payrollId };
            var payroll = new Payroll { Id = payrollId, Status = PayrollStatus.Draft, CalculatedSalary = 10000000 };

            _salaryDetailRepoMock.Setup(r => r.GetByIdAsync(detailId)).ReturnsAsync(existingDetail);
            _payrollRepoMock.Setup(r => r.GetByIdAsync(payrollId)).ReturnsAsync(payroll);
            _salaryDetailRepoMock.Setup(r => r.DeleteAsync(detailId)).Returns(Task.CompletedTask);

            // Mock recalculate: after deletion, list of details is empty
            _salaryDetailRepoMock.Setup(r => r.GetAllAsync(payrollId, null, null, null)).ReturnsAsync(new List<SalaryDetail>());
            _payrollRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Payroll>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(detailId);

            // Assert
            Assert.True(result);
            _salaryDetailRepoMock.Verify(r => r.DeleteAsync(detailId), Times.Once);
            _payrollRepoMock.Verify(r => r.UpdateAsync(It.Is<Payroll>(p =>
                p.Id == payrollId &&
                p.TotalAllowance == 0 &&
                p.BonusAmount == 0 &&
                p.TotalDeduction == 0 &&
                p.PenaltyAmount == 0 &&
                p.NetSalary == 10000000)), Times.Once);
        }

        #endregion
    }
}
