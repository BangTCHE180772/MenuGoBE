using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IProductRepository
    {
        Task<List<Product>> GetByTypeAsync(string type);
        Task<Product?> GetByIdAndTypeAsync(long id, string type);
        Task CreateAsync(Product entity);
        Task UpdateAsync(Product entity);
        Task DeleteAsync(Product entity);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameExcludeIdAsync(string name, long id);
        Task<bool> ExistsBySKUAsync(string sku);
        Task<bool> ExistsBySKUExcludeIdAsync(string sku, long id);
        Task<bool> UnitExistsAsync(long unitId);
        Task ClearUnitConversionsAsync(long productId);
        Task<string?> GetProductTypeAsync(long id);
        Task SaveChangesAsync();
    }
}
