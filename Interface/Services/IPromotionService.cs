using MenuGoBE.Dtos.Promotion;

namespace MenuGoBE.Interface.Services;

public interface IPromotionService
{
    Task<PromotionDto> CreatePromotionAsync(PromotionCreateDto dto, long userId);
    Task<PromotionDto?> GetPromotionByIdAsync(long id);
    Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync();
    Task<IEnumerable<PromotionDto>> GetAllPromotionsByBranchAsync(long branchId);
    Task<PromotionDto> UpdatePromotionAsync(PromotionUpdateDto dto, long userId);
    Task DeletePromotionAsync(long id);
    Task RemoveProductFromPromotionAsync(long promoId, long productId);
    Task<Dictionary<long, PromotionDto>> GetActivePromotionMapForBranchAsync(long branchId);
}
