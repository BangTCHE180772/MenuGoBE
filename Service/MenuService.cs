using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Menu;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _repo;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public MenuService(IMenuRepository repo, IMapper mapper, AppDbContext context)
        {
            _repo = repo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<List<MenuViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<MenuViewDto>>(data);
        }

        public async Task<MenuViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<MenuViewDto>(data);
        }

        public async Task<MenuViewDto> CreateAsync(MenuCreateDto dto)
        {
            if (await _repo.ExistsByNameAsync(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.DuplicateMenuName);
            }

            var entity = _mapper.Map<Menu>(dto);
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();
            return _mapper.Map<MenuViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(MenuUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (await _repo.ExistsByNameExcludeIdAsync(dto.Name, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateMenuName);
            }

            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
            return true;
        }

        // ── Single product ───────────────────────────────────────────────────

        public async Task<(bool success, string message)> AddProductAsync(MenuProductDto dto)
        {
            var menuExists = await _repo.GetByIdAsync(dto.MenuId) != null;
            if (!menuExists)
                return (false, $"Không tìm thấy menu với Id = {dto.MenuId}.");

            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
            if (!productExists)
                return (false, $"Không tìm thấy product với Id = {dto.ProductId}.");

            var alreadyIn = await _repo.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId);
            if (alreadyIn)
                return (false, "Product này đã có trong menu rồi.");

            await _repo.AddProductAsync(dto.MenuId, dto.ProductId);
            await _repo.SaveChangesAsync();
            return (true, "Thêm product vào menu thành công.");
        }

        public async Task<(bool success, string message)> RemoveProductAsync(MenuProductDto dto)
        {
            var menuExists = await _repo.GetByIdAsync(dto.MenuId) != null;
            if (!menuExists)
                return (false, $"Không tìm thấy menu với Id = {dto.MenuId}.");

            var alreadyIn = await _repo.ProductExistsInMenuAsync(dto.MenuId, dto.ProductId);
            if (!alreadyIn)
                return (false, "Product này không có trong menu.");

            await _repo.RemoveProductAsync(dto.MenuId, dto.ProductId);
            await _repo.SaveChangesAsync();
            return (true, "Xóa product khỏi menu thành công.");
        }

        // ── Bulk products ────────────────────────────────────────────────────

        public async Task<(bool success, string message)> AddProductsAsync(MenuProductsBulkDto dto)
        {
            if (dto.ProductIds == null || dto.ProductIds.Count == 0)
                return (false, "Danh sách product không được rỗng.");

            var menuExists = await _repo.GetByIdAsync(dto.MenuId) != null;
            if (!menuExists)
                return (false, $"Không tìm thấy menu với Id = {dto.MenuId}.");

            // Kiểm tra tất cả productIds có tồn tại không
            var distinctIds = dto.ProductIds.Distinct().ToList();
            var foundCount = await _context.Products
                .Where(p => distinctIds.Contains(p.Id))
                .CountAsync();

            if (foundCount != distinctIds.Count)
                return (false, "Một hoặc nhiều productId không tồn tại trong hệ thống.");

            await _repo.AddProductsAsync(dto.MenuId, distinctIds);
            await _repo.SaveChangesAsync();
            return (true, $"Thêm thành công {distinctIds.Count} product vào menu (đã bỏ qua các product đã tồn tại).");
        }

        public async Task<(bool success, string message)> RemoveProductsAsync(MenuProductsBulkDto dto)
        {
            if (dto.ProductIds == null || dto.ProductIds.Count == 0)
                return (false, "Danh sách product không được rỗng.");

            var menuExists = await _repo.GetByIdAsync(dto.MenuId) != null;
            if (!menuExists)
                return (false, $"Không tìm thấy menu với Id = {dto.MenuId}.");

            await _repo.RemoveProductsAsync(dto.MenuId, dto.ProductIds.Distinct());
            await _repo.SaveChangesAsync();
            return (true, "Xóa các product khỏi menu thành công.");
        }

        public async Task<List<MenuProductViewDto>> GetProductsAsync(long menuId)
        {
            var products = await _context.MenuProducts
                .Where(mp => mp.MenuId == menuId && mp.Product.IsSellable &&
                             (mp.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed ||
                              mp.Product.Type == MenuGoBE.Models.Enums.ProductType.Manufactured ||
                              mp.Product.Type == MenuGoBE.Models.Enums.ProductType.Regular))
                .Include(mp => mp.Product)
                    .ThenInclude(p => p.Group)
                .Include(mp => mp.Product)
                    .ThenInclude(p => p.Image)
                .Select(mp => new MenuProductViewDto
                {
                    Id = mp.Product.Id,
                    Name = mp.Product.Name,
                    Description = mp.Product.Description,
                    Type = mp.Product.Type,
                    SellPrice = mp.Product.SellPrice,
                    IsSellable = mp.Product.IsSellable,
                    GroupId = mp.Product.GroupId,
                    GroupName = mp.Product.Group != null ? mp.Product.Group.Name : string.Empty,
                    ImageLink = mp.Product.Image != null ? mp.Product.Image.ImageLink : string.Empty,
                })
                .OrderBy(p => p.GroupId)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return products;
        }
    }
}
