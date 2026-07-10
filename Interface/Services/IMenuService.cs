using MenuGoBE.Dtos.Menu;

namespace MenuGoBE.Interface.Services
{
    public interface IMenuService
    {
        Task<List<MenuViewDto>> GetAllAsync();
        Task<MenuViewDto?> GetByIdAsync(long id);
        Task<MenuViewDto> CreateAsync(MenuCreateDto dto);
        Task<bool> UpdateAsync(MenuUpdateDto dto);
        Task<bool> DeleteAsync(long id);

        // Single product
        Task<(bool success, string message)> AddProductAsync(MenuProductDto dto);
        Task<(bool success, string message)> RemoveProductAsync(MenuProductDto dto);

        // Bulk products
        Task<(bool success, string message)> AddProductsAsync(MenuProductsBulkDto dto);
        Task<(bool success, string message)> RemoveProductsAsync(MenuProductsBulkDto dto);

        Task<List<MenuProductViewDto>> GetProductsAsync(long menuId);
    }
}
