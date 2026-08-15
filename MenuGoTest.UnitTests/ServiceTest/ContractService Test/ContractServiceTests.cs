using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Contract;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.ContractServiceTest
{
    public class ContractServiceTests
    {
        private readonly Mock<IContractRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _repoMock = new Mock<IContractRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new ContractService(_repoMock.Object, _mapper);
        }

        #region GetAllAsync

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedContractViewDtos()
        {
            // Arrange
            var contracts = new List<Contract>
            {
                new Contract { Id = 1, AccountId = 10, BranchId = 1, BaseSalary = 10000000 },
                new Contract { Id = 2, AccountId = 11, BranchId = 2, BaseSalary = 12000000 }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(contracts);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetByBranchIdsAsync

        [Fact]
        public async Task GetByBranchIdsAsync_ShouldReturnMappedContractViewDtos()
        {
            // Arrange
            var branchIds = new List<long> { 1, 2 };
            var contracts = new List<Contract>
            {
                new Contract { Id = 1, AccountId = 10, BranchId = 1, BaseSalary = 8000000 }
            };

            _repoMock.Setup(r => r.GetByBranchIdsAsync(branchIds)).ReturnsAsync(contracts);

            // Act
            var result = await _service.GetByBranchIdsAsync(branchIds);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            _repoMock.Verify(r => r.GetByBranchIdsAsync(branchIds), Times.Once);
        }

        #endregion

        #region GetContractsByRolesAsync

        [Fact]
        public async Task GetContractsByRolesAsync_ShouldReturnMappedContractViewDtos()
        {
            // Arrange
            var roleIds = new List<long> { 4, 5 };
            long branchId = 1;
            var contracts = new List<Contract>
            {
                new Contract { Id = 5, AccountId = 15, RoleId = 4, BranchId = branchId }
            };

            _repoMock.Setup(r => r.GetContractsByRolesAsync(roleIds, branchId, null)).ReturnsAsync(contracts);

            // Act
            var result = await _service.GetContractsByRolesAsync(roleIds, branchId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(5, result[0].Id);
            _repoMock.Verify(r => r.GetContractsByRolesAsync(roleIds, branchId, null), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenContractExists_ShouldReturnMappedDto()
        {
            // Arrange
            long contractId = 1;
            var contract = new Contract { Id = contractId, AccountId = 10, BaseSalary = 15000000 };

            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

            // Act
            var result = await _service.GetByIdAsync(contractId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contractId, result.Id);
            Assert.Equal(10, result.AccountId);
            _repoMock.Verify(r => r.GetByIdAsync(contractId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenContractNotFound_ShouldReturnNull()
        {
            // Arrange
            long contractId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync((Contract?)null);

            // Act
            var result = await _service.GetByIdAsync(contractId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(contractId), Times.Once);
        }

        #endregion

        #region GetByAccountIdAsync

        [Fact]
        public async Task GetByAccountIdAsync_ShouldReturnMappedContractViewDtos()
        {
            // Arrange
            long accountId = 10;
            var contracts = new List<Contract>
            {
                new Contract { Id = 1, AccountId = accountId, Type = "Full-time" },
                new Contract { Id = 2, AccountId = accountId, Type = "Part-time" }
            };

            _repoMock.Setup(r => r.GetByAccountIdAsync(accountId)).ReturnsAsync(contracts);

            // Act
            var result = await _service.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(accountId, c.AccountId));
            _repoMock.Verify(r => r.GetByAccountIdAsync(accountId), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WhenValidDto_ShouldCreateAndReturnContractViewDto()
        {
            // Arrange
            var dto = new ContractCreateDto
            {
                AccountId = 10,
                RoleId = 6, // Staff Waiter
                BranchId = 1,
                Type = "Full-time",
                SalaryType = "Fixed",
                BaseSalary = 10000000,
                Status = "Active"
            };

            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(dto.AccountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsManagerRoleAsync(dto.RoleId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.AccountId, result.AccountId);
            Assert.Equal(dto.BaseSalary, result.BaseSalary);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Contract>(c =>
                c.AccountId == dto.AccountId &&
                c.RoleId == dto.RoleId &&
                c.BranchId == dto.BranchId &&
                c.BaseSalary == dto.BaseSalary)), Times.Once);

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenAccountAlreadyHasActiveContract_ShouldThrowException()
        {
            // Arrange
            var dto = new ContractCreateDto
            {
                AccountId = 10,
                RoleId = 6,
                BranchId = 1,
                Status = "Active"
            };

            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(dto.AccountId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(dto));
            Assert.Contains("Tài khoản này hiện đã có hợp đồng đang có hiệu lực", ex.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenCreatingActiveManagerContractAndBranchAlreadyHasActiveManager_ShouldThrowException()
        {
            // Arrange
            var dto = new ContractCreateDto
            {
                AccountId = 20,
                RoleId = 3, // Manager
                BranchId = 1,
                Status = "Active"
            };

            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(dto.AccountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsManagerRoleAsync(dto.RoleId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.HasActiveManagerAsync(dto.BranchId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(dto));
            Assert.Contains("Chi nhánh này đã có Quản lý", ex.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenCreatingActiveManagerContractAndBranchHasNoActiveManager_ShouldSucceed()
        {
            // Arrange
            var dto = new ContractCreateDto
            {
                AccountId = 20,
                RoleId = 3, // Manager
                BranchId = 1,
                Status = "Active",
                BaseSalary = 20000000
            };

            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(dto.AccountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsManagerRoleAsync(dto.RoleId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.HasActiveManagerAsync(dto.BranchId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.AccountId, result.AccountId);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Contract>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenContractExistsAndValid_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            long contractId = 1;
            var existingContract = new Contract
            {
                Id = contractId,
                AccountId = 10,
                RoleId = 6,
                BranchId = 1,
                Status = "Active",
                BaseSalary = 10000000
            };

            var updateDto = new ContractUpdateDto
            {
                Id = contractId,
                AccountId = 10,
                RoleId = 6,
                BranchId = 1,
                Status = "Active",
                BaseSalary = 12000000
            };

            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(existingContract);
            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(updateDto.AccountId, contractId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsManagerRoleAsync(updateDto.RoleId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.UpdateAsync(existingContract), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenContractNotFound_ShouldReturnFalse()
        {
            // Arrange
            var updateDto = new ContractUpdateDto { Id = 999, AccountId = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Contract?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenStatusActiveAndAccountHasOtherActiveContract_ShouldThrowException()
        {
            // Arrange
            long contractId = 1;
            var existingContract = new Contract { Id = contractId, AccountId = 10, Status = "Expired" };
            var updateDto = new ContractUpdateDto
            {
                Id = contractId,
                AccountId = 10,
                Status = "Active"
            };

            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(existingContract);
            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(updateDto.AccountId, contractId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(updateDto));
            Assert.Contains("Tài khoản này hiện đã có hợp đồng khác đang có hiệu lực", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenStatusActiveAndRoleIsManagerAndBranchHasOtherActiveManager_ShouldThrowException()
        {
            // Arrange
            long contractId = 1;
            var existingContract = new Contract { Id = contractId, AccountId = 20, RoleId = 3, BranchId = 1, Status = "Expired" };
            var updateDto = new ContractUpdateDto
            {
                Id = contractId,
                AccountId = 20,
                RoleId = 3, // Manager
                BranchId = 1,
                Status = "Active"
            };

            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(existingContract);
            _repoMock.Setup(r => r.HasActiveContractByAccountAsync(updateDto.AccountId, contractId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.IsManagerRoleAsync(updateDto.RoleId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.HasActiveManagerAsync(updateDto.BranchId, contractId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(updateDto));
            Assert.Contains("Chi nhánh này đã có Quản lý", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Contract>()), Times.Never);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_WhenContractExists_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            long contractId = 1;
            var existingContract = new Contract { Id = contractId, AccountId = 10 };

            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(existingContract);
            _repoMock.Setup(r => r.DeleteAsync(contractId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(contractId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenContractNotFound_ShouldReturnFalse()
        {
            // Arrange
            long contractId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync((Contract?)null);

            // Act
            var result = await _service.DeleteAsync(contractId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        #endregion
    }
}
