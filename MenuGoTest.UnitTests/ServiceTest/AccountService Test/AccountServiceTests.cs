using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Account;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.AccountServiceTest
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _repoMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly IMapper _mapper;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _repoMock = new Mock<IAccountRepository>();
            _emailServiceMock = new Mock<IEmailService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new AccountService(_repoMock.Object, _mapper, _emailServiceMock.Object);
        }

        #region GetAccountsExcludeRolesAsync

        [Fact]
        public async Task GetAccountsExcludeRolesAsync_ShouldReturnMappedAccountViewDtos()
        {
            // Arrange
            var excludedRoles = new List<long> { 1, 2 };
            var accounts = new List<Account>
            {
                new Account { Id = 10, Name = "User 10", Email = "u10@test.com" },
                new Account { Id = 11, Name = "User 11", Email = "u11@test.com" }
            };

            _repoMock.Setup(r => r.GetAccountsExcludeRolesAsync(excludedRoles, null, null))
                     .ReturnsAsync(accounts);

            // Act
            var result = await _service.GetAccountsExcludeRolesAsync(excludedRoles);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("User 10", result[0].Name);
            Assert.Equal("User 11", result[1].Name);
            _repoMock.Verify(r => r.GetAccountsExcludeRolesAsync(excludedRoles, null, null), Times.Once);
        }

        #endregion

        #region GetAccountsByRolesAsync

        [Fact]
        public async Task GetAccountsByRolesAsync_ShouldReturnMappedAccountViewDtos()
        {
            // Arrange
            var roleIds = new List<long> { 4, 5 };
            var branchId = 2L;
            var accounts = new List<Account>
            {
                new Account { Id = 20, Name = "Staff 20", Email = "staff20@test.com" }
            };

            _repoMock.Setup(r => r.GetAccountsByRolesAsync(roleIds, branchId))
                     .ReturnsAsync(accounts);

            // Act
            var result = await _service.GetAccountsByRolesAsync(roleIds, branchId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Staff 20", result[0].Name);
            _repoMock.Verify(r => r.GetAccountsByRolesAsync(roleIds, branchId), Times.Once);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenAccountExists_ShouldReturnMappedDto()
        {
            // Arrange
            long accountId = 1;
            var account = new Account { Id = accountId, Name = "John Doe", Email = "john@test.com" };

            _repoMock.Setup(r => r.GetByIdAsync(accountId))
                     .ReturnsAsync(account);

            // Act
            var result = await _service.GetByIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result.Id);
            Assert.Equal("John Doe", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenAccountNotFound_ShouldReturnNull()
        {
            // Arrange
            long accountId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(accountId))
                     .ReturnsAsync((Account?)null);

            // Act
            var result = await _service.GetByIdAsync(accountId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(accountId), Times.Once);
        }

        #endregion

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WhenValidDto_ShouldHashPasswordSaveAndSendEmail()
        {
            // Arrange
            var dto = new AccountCreateDto
            {
                Name = "New User",
                Email = "newuser@test.com",
                Phone = "0912345678",
                CitizenIdCode = "012345678901",
                Password = "Password123!",
                AvatarImage = "http://example.com/avatar.png"
            };

            _repoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(dto.Phone, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(dto, 5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Email, result.Email);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Account>(a =>
                a.Name == dto.Name &&
                a.Email == dto.Email &&
                a.CreatedBy == 5 &&
                !string.IsNullOrEmpty(a.HashedPassword) &&
                a.HashedPassword != dto.Password)), Times.Once);

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenEmailExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new AccountCreateDto
            {
                Name = "New User",
                Email = "existing@test.com",
                Phone = "0912345678",
                CitizenIdCode = "012345678901",
                Password = "Password123!"
            };

            _repoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
            Assert.Equal("Email đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPhoneExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new AccountCreateDto
            {
                Name = "New User",
                Email = "new@test.com",
                Phone = "0912345678",
                CitizenIdCode = "012345678901",
                Password = "Password123!"
            };

            _repoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(dto.Phone, null)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
            Assert.Equal("Số điện thoại đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenCitizenIdExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new AccountCreateDto
            {
                Name = "New User",
                Email = "new@test.com",
                Phone = "0912345678",
                CitizenIdCode = "012345678901",
                Password = "Password123!"
            };

            _repoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(dto.Phone, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode, null)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
            Assert.Equal("Số CCCD đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEmailSendingFails_ShouldNotThrowException()
        {
            // Arrange
            var dto = new AccountCreateDto
            {
                Name = "User Email Fail",
                Email = "emailfail@test.com",
                Phone = "0912345678",
                CitizenIdCode = "012345678901",
                Password = "Password123!",
                AvatarImage = "http://example.com/avatar.png"
            };

            _repoMock.Setup(r => r.ExistsByEmailAsync(dto.Email, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(dto.Phone, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByCitizenIdCodeAsync(dto.CitizenIdCode, null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenAccountExistsAndValid_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            long accountId = 1;
            var existingAccount = new Account { Id = accountId, Name = "Old Name", Email = "old@test.com" };
            var updateDto = new AccountUpdateDto
            {
                Id = accountId,
                Name = "Updated Name",
                Email = "updated@test.com",
                Phone = "0987654321",
                CitizenIdCode = "987654321098",
                AvatarImage = "http://example.com/newavatar.png"
            };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);
            _repoMock.Setup(r => r.ExistsByEmailAsync(updateDto.Email, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(updateDto.Phone, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByCitizenIdCodeAsync(updateDto.CitizenIdCode, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.UpdateAsync(existingAccount), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenAccountNotFound_ShouldReturnFalse()
        {
            // Arrange
            var updateDto = new AccountUpdateDto { Id = 999, Name = "Non Existent" };
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Account?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailExistsForOtherAccount_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 1;
            var existingAccount = new Account { Id = accountId, Name = "User 1", Email = "user1@test.com" };
            var updateDto = new AccountUpdateDto
            {
                Id = accountId,
                Email = "duplicate@test.com"
            };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);
            _repoMock.Setup(r => r.ExistsByEmailAsync(updateDto.Email, accountId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal("Email đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenPhoneExistsForOtherAccount_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 1;
            var existingAccount = new Account { Id = accountId, Name = "User 1" };
            var updateDto = new AccountUpdateDto
            {
                Id = accountId,
                Email = "valid@test.com",
                Phone = "0987654321"
            };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);
            _repoMock.Setup(r => r.ExistsByEmailAsync(updateDto.Email, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(updateDto.Phone, accountId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal("Số điện thoại đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenCitizenIdExistsForOtherAccount_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 1;
            var existingAccount = new Account { Id = accountId, Name = "User 1" };
            var updateDto = new AccountUpdateDto
            {
                Id = accountId,
                Email = "valid@test.com",
                Phone = "0987654321",
                CitizenIdCode = "123456789012"
            };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);
            _repoMock.Setup(r => r.ExistsByEmailAsync(updateDto.Email, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByPhoneAsync(updateDto.Phone, accountId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsByCitizenIdCodeAsync(updateDto.CitizenIdCode, accountId)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal("Số CCCD đã được sử dụng.", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_WhenAccountExists_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            long accountId = 1;
            var account = new Account { Id = accountId, Name = "User To Delete" };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _repoMock.Setup(r => r.DeleteAsync(accountId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(accountId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.DeleteAsync(accountId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenAccountHasActiveContract_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 1;
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var account = new Account
            {
                Id = accountId,
                Name = "Active Contract User",
                Contracts = new List<Contract>
                {
                    new Contract
                    {
                        Status = "Active",
                        StartDate = today.AddDays(-10),
                        EndDate = today.AddDays(30)
                    }
                }
            };

            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(accountId));
            Assert.Contains("không thể xóa tài khoản", ex.Message.ToLower());
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }

        #endregion

        #region ChangePasswordAsync

        [Fact]
        public async Task ChangePasswordAsync_WhenAccountNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 999;
            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangePasswordAsync(accountId, "old", "new"));
            Assert.Equal("Không tìm thấy tài khoản.", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenCurrentPasswordWrong_ShouldThrowInvalidOperationException()
        {
            // Arrange
            long accountId = 1;
            var account = new Account { Id = accountId, HashedPassword = MenuGoBE.Helpers.HashHelper.HashPassword("CorrectPass123") };
            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangePasswordAsync(accountId, "WrongPass", "NewPass123"));
            Assert.Equal("Mật khẩu hiện tại không đúng.", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenCurrentPasswordCorrect_ShouldHashAndSaveNewPassword()
        {
            // Arrange
            long accountId = 1;
            var oldHash = MenuGoBE.Helpers.HashHelper.HashPassword("CorrectPass123");
            var account = new Account { Id = accountId, HashedPassword = oldHash };
            _repoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.ChangePasswordAsync(accountId, "CorrectPass123", "NewPass456");

            // Assert
            Assert.NotEqual(oldHash, account.HashedPassword);
            Assert.True(MenuGoBE.Helpers.HashHelper.VerifyPassword("NewPass456", account.HashedPassword));
            _repoMock.Verify(r => r.UpdateAsync(account), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetAccountPermissionsAsync

        [Fact]
        public async Task GetAccountPermissionsAsync_ShouldReturnPermissionsFromRepo()
        {
            // Arrange
            long accountId = 10;
            var expectedPermissions = (IsAdmin: false, IsManager: true, BranchIds: new List<long> { 1, 2 });

            _repoMock.Setup(r => r.GetAccountPermissionsAsync(accountId))
                     .ReturnsAsync(expectedPermissions);

            // Act
            var result = await _service.GetAccountPermissionsAsync(accountId);

            // Assert
            Assert.False(result.IsAdmin);
            Assert.True(result.IsManager);
            Assert.Equal(2, result.BranchIds.Count);
            _repoMock.Verify(r => r.GetAccountPermissionsAsync(accountId), Times.Once);
        }

        #endregion

        #region CanManagerAccessAccountAsync

        [Fact]
        public async Task CanManagerAccessAccountAsync_ShouldReturnResultFromRepo()
        {
            // Arrange
            long targetAccountId = 15;
            var managerBranchIds = new List<long> { 1, 2 };
            long managerAccountId = 2;

            _repoMock.Setup(r => r.CanManagerAccessAccountAsync(targetAccountId, managerBranchIds, managerAccountId))
                     .ReturnsAsync(true);

            // Act
            var result = await _service.CanManagerAccessAccountAsync(targetAccountId, managerBranchIds, managerAccountId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.CanManagerAccessAccountAsync(targetAccountId, managerBranchIds, managerAccountId), Times.Once);
        }

        #endregion
    }
}
