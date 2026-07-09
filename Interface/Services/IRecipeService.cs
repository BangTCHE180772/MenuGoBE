using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Product;

namespace MenuGoBE.Interface.Services
{
    public interface IRecipeService
    {
        Task<IEnumerable<ProductRecipeViewDto>> GetAllByProductAsync(long productId);
        Task<ProductRecipeViewDto> CreateAsync(long productId, ProductRecipeDto dto);
        Task<ProductRecipeViewDto> UpdateAsync(long productId, long recipeId, ProductRecipeDto dto);
        Task<bool> DeleteAsync(long productId, long recipeId);
    }
}
