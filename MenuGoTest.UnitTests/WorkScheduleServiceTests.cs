using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
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

            _service = new WorkScheduleService(
                _repoMock.Object, 
                _mapper,
                _branchRepoMock.Object,
                _shiftRepoMock.Object,
                _accountRepoMock.Object);
        }

        [Fact]
        public async Task Test_GetByAccountIdAsync_ReturnsWorkSchedules()
        {
            // Arrange
            var accountId = 10L;
            var schedules = new List<WorkSchedule>
            {
                new WorkSchedule { Id = 1, AccountId = accountId, BranchId = 1, ShiftId = 2, WorkDate = new DateOnly(2026, 7, 20), Code = "WS-1", Status = "APPROVED" },
                new WorkSchedule { Id = 2, AccountId = accountId, BranchId = 1, ShiftId = 3, WorkDate = new DateOnly(2026, 7, 21), Code = "WS-2", Status = "PENDING" }
            };
            _repoMock.Setup(r => r.GetByAccountIdAsync(accountId)).ReturnsAsync(schedules);

            // Act
            var result = await _service.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("WS-1", result[0].Code);
            Assert.Equal("WS-2", result[1].Code);
            _repoMock.Verify(r => r.GetByAccountIdAsync(accountId), Times.Once);
        }
    }
}
