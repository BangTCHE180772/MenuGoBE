using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IMenuRepository
    {
        Task<List<Menu>> GetAllAsync();
        Task<Menu?> GetByIdAsync(long id);
        Task<Menu?> GetByIdWithProductsAsync(long id);

        Task CreateAsync(Menu entity);
        Task UpdateAsync(Menu entity);
        Task DeleteAsync(long id);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludeIdAsync(string name, long id);

        // Product membership
        Task<bool> ProductExistsInMenuAsync(long menuId, long productId);
        Task AddProductAsync(long menuId, long productId);
        Task RemoveProductAsync(long menuId, long productId);
        Task AddProductsAsync(long menuId, IEnumerable<long> productIds);
        Task RemoveProductsAsync(long menuId, IEnumerable<long> productIds);

        Task SaveChangesAsync();
    }
}
