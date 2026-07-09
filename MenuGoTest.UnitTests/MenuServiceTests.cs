using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Menu;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests
{
    public class MenuServiceTests : IDisposable
    {
        private readonly Mock<IMenuRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly MenuService _service;

        public MenuServiceTests()
        {
            _repoMock = new Mock<IMenuRepository>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _service = new MenuService(_repoMock.Object, _mapper, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos_WhenMenusExist()
        {
            // Arrange
            var menus = new List<Menu>
            {
                new Menu { Id = 1, Name = "Breakfast" },
                new Menu { Id = 2, Name = "Lunch" }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(menus);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Breakfast", result[0].Name);
            Assert.Equal("Lunch", result[1].Name);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenMenuExists()
        {
            // Arrange
            var menuId = 1L;
            var menu = new Menu { Id = menuId, Name = "Breakfast" };
            _repoMock.Setup(r => r.GetByIdAsync(menuId)).ReturnsAsync(menu);

            // Act
            var result = await _service.GetByIdAsync(menuId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Breakfast", result.Name);
            _repoMock.Verify(r => r.GetByIdAsync(menuId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenMenuDoesNotExist()
        {
            // Arrange
            var menuId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(menuId)).ReturnsAsync((Menu?)null);

            // Act
            var result = await _service.GetByIdAsync(menuId);

            // Assert
            Assert.Null(result);
            _repoMock.Verify(r => r.GetByIdAsync(menuId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto_WhenNameIsUnique()
        {
            // Arrange
            var createDto = new MenuCreateDto { Name = "Dinner" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(false);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Menu>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Dinner", result.Name);
            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Menu>(m => m.Name == createDto.Name)), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowDuplicateMenuNameException_WhenNameAlreadyExists()
        {
            // Arrange
            var createDto = new MenuCreateDto { Name = "Breakfast" };
            _repoMock.Setup(r => r.ExistsByNameAsync(createDto.Name)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.CreateAsync(createDto));
            Assert.Equal(ErrorCodes.DuplicateMenuName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateMenuName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.ExistsByNameAsync(createDto.Name), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Menu>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnTrue_WhenMenuExistsAndNameIsUnique()
        {
            // Arrange
            var updateDto = new MenuUpdateDto { Id = 1, Name = "Breakfast Updated" };
            var existingMenu = new Menu { Id = 1, Name = "Breakfast" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingMenu);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Menu>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Breakfast Updated", existingMenu.Name);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(existingMenu), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenMenuDoesNotExist()
        {
            // Arrange
            var updateDto = new MenuUpdateDto { Id = 99, Name = "Non Existent" };
            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((Menu?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Menu>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowDuplicateMenuNameException_WhenNameExistsOnAnotherMenu()
        {
            // Arrange
            var updateDto = new MenuUpdateDto { Id = 1, Name = "Lunch" };
            var existingMenu = new Menu { Id = 1, Name = "Breakfast" };

            _repoMock.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync(existingMenu);
            _repoMock.Setup(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.UpdateAsync(updateDto));
            Assert.Equal(ErrorCodes.DuplicateMenuName.Code, ex.Code);
            Assert.Equal(ErrorCodes.DuplicateMenuName.HttpStatusCode, ex.HttpStatusCode);

            _repoMock.Verify(r => r.GetByIdAsync(updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.ExistsByNameExcludeIdAsync(updateDto.Name, updateDto.Id), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Menu>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenMenuExists()
        {
            // Arrange
            var menuId = 1L;
            var existingMenu = new Menu { Id = menuId, Name = "Breakfast" };

            _repoMock.Setup(r => r.GetByIdAsync(menuId)).ReturnsAsync(existingMenu);
            _repoMock.Setup(r => r.DeleteAsync(menuId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(menuId);

            // Assert
            Assert.True(result);
            _repoMock.Verify(r => r.GetByIdAsync(menuId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(menuId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenMenuDoesNotExist()
        {
            // Arrange
            var menuId = 99L;
            _repoMock.Setup(r => r.GetByIdAsync(menuId)).ReturnsAsync((Menu?)null);

            // Act
            var result = await _service.DeleteAsync(menuId);

            // Assert
            Assert.False(result);
            _repoMock.Verify(r => r.GetByIdAsync(menuId), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        // ── Single Product Tests ─────────────────────────────────────────────

        [Fact]
        public async Task AddProductAsync_ShouldReturnSuccess_WhenMenuAndProductExistAndNotAlreadyAdded()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 1, ProductId = 10 };
            
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId)).ReturnsAsync(false);
            _repoMock.Setup(r => r.AddProductAsync(dto.MenuId, dto.ProductId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Seed Product to DbContext
            _context.Products.Add(new Product { Id = dto.ProductId, Name = "Sample Product" });
            await _context.SaveChangesAsync();

            // Act
            var (success, message) = await _service.AddProductAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Equal("Thêm product vào menu thành công.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.AddProductAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddProductAsync_ShouldReturnFailure_WhenMenuDoesNotExist()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 99, ProductId = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync((Menu?)null);

            // Act
            var (success, message) = await _service.AddProductAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal($"Không tìm thấy menu với Id = {dto.MenuId}.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task AddProductAsync_ShouldReturnFailure_WhenProductDoesNotExist()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 1, ProductId = 99 };
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            
            // Database is empty (Product doesn't exist)

            // Act
            var (success, message) = await _service.AddProductAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal($"Không tìm thấy product với Id = {dto.ProductId}.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task AddProductAsync_ShouldReturnFailure_WhenProductAlreadyExistsInMenu()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 1, ProductId = 10 };
            
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId)).ReturnsAsync(true);

            // Seed Product to DbContext
            _context.Products.Add(new Product { Id = dto.ProductId, Name = "Sample Product" });
            await _context.SaveChangesAsync();

            // Act
            var (success, message) = await _service.AddProductAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("Product này đã có trong menu rồi.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.AddProductAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RemoveProductAsync_ShouldReturnSuccess_WhenMenuExistsAndProductInMenu()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 1, ProductId = 10 };
            
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.RemoveProductAsync(dto.MenuId, dto.ProductId)).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var (success, message) = await _service.RemoveProductAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Equal("Xóa product khỏi menu thành công.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.RemoveProductAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveProductAsync_ShouldReturnFailure_WhenMenuDoesNotExist()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 99, ProductId = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync((Menu?)null);

            // Act
            var (success, message) = await _service.RemoveProductAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal($"Không tìm thấy menu với Id = {dto.MenuId}.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RemoveProductAsync_ShouldReturnFailure_WhenProductIsNotInMenu()
        {
            // Arrange
            var dto = new MenuProductDto { MenuId = 1, ProductId = 10 };
            
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId)).ReturnsAsync(false);

            // Act
            var (success, message) = await _service.RemoveProductAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("Product này không có trong menu.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId), Times.Once);
            _repoMock.Verify(r => r.RemoveProductAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        }

        // ── Bulk Products Tests ──────────────────────────────────────────────

        [Fact]
        public async Task AddProductsAsync_ShouldReturnSuccess_WhenMenuAndProductsExist()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 1,
                ProductIds = new List<long> { 101, 102 }
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.AddProductsAsync(dto.MenuId, It.IsAny<IEnumerable<long>>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Seed Products
            _context.Products.AddRange(new List<Product>
            {
                new Product { Id = 101, Name = "Prod A" },
                new Product { Id = 102, Name = "Prod B" }
            });
            await _context.SaveChangesAsync();

            // Act
            var (success, message) = await _service.AddProductsAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Contains("Thêm thành công 2 product vào menu", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.AddProductsAsync(dto.MenuId, It.Is<IEnumerable<long>>(ids => ids.Contains(101) && ids.Contains(102))), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddProductsAsync_ShouldReturnFailure_WhenProductListIsEmpty()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 1,
                ProductIds = new List<long>()
            };

            // Act
            var (success, message) = await _service.AddProductsAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("Danh sách product không được rỗng.", message);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task AddProductsAsync_ShouldReturnFailure_WhenMenuDoesNotExist()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 99,
                ProductIds = new List<long> { 101 }
            };
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync((Menu?)null);

            // Act
            var (success, message) = await _service.AddProductsAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal($"Không tìm thấy menu với Id = {dto.MenuId}.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
        }

        [Fact]
        public async Task AddProductsAsync_ShouldReturnFailure_WhenOneOrMoreProductsDoNotExist()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 1,
                ProductIds = new List<long> { 101, 999 } // 999 is missing
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });

            // Seed only 101
            _context.Products.Add(new Product { Id = 101, Name = "Prod A" });
            await _context.SaveChangesAsync();

            // Act
            var (success, message) = await _service.AddProductsAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("Một hoặc nhiều productId không tồn tại trong hệ thống.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.AddProductsAsync(It.IsAny<long>(), It.IsAny<IEnumerable<long>>()), Times.Never);
        }

        [Fact]
        public async Task RemoveProductsAsync_ShouldReturnSuccess_WhenMenuExists()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 1,
                ProductIds = new List<long> { 101, 102 }
            };

            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync(new Menu { Id = dto.MenuId });
            _repoMock.Setup(r => r.RemoveProductsAsync(dto.MenuId, It.IsAny<IEnumerable<long>>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var (success, message) = await _service.RemoveProductsAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Equal("Xóa các product khỏi menu thành công.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.RemoveProductsAsync(dto.MenuId, It.Is<IEnumerable<long>>(ids => ids.Contains(101) && ids.Contains(102))), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveProductsAsync_ShouldReturnFailure_WhenProductListIsEmpty()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 1,
                ProductIds = new List<long>()
            };

            // Act
            var (success, message) = await _service.RemoveProductsAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("Danh sách product không được rỗng.", message);
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RemoveProductsAsync_ShouldReturnFailure_WhenMenuDoesNotExist()
        {
            // Arrange
            var dto = new MenuProductsBulkDto
            {
                MenuId = 99,
                ProductIds = new List<long> { 101 }
            };
            _repoMock.Setup(r => r.GetByIdAsync(dto.MenuId)).ReturnsAsync((Menu?)null);

            // Act
            var (success, message) = await _service.RemoveProductsAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal($"Không tìm thấy menu với Id = {dto.MenuId}.", message);
            _repoMock.Verify(r => r.GetByIdAsync(dto.MenuId), Times.Once);
            _repoMock.Verify(r => r.RemoveProductsAsync(It.IsAny<long>(), It.IsAny<IEnumerable<long>>()), Times.Never);
        }
    }
}
