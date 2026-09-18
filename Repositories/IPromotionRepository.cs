using MenuGoBE.Models;

namespace MenuGoBE.Repositories;

public interface IPromotionRepository
{
    Task<Promotion> CreatePromotionAsync(Promotion promotion);
    Task<Promotion?> GetPromotionByIdAsync(long id);
    Task<IEnumerable<Promotion>> GetAllPromotionsAsync();
    Task<IEnumerable<Promotion>> GetAllPromotionsByBranchAsync(long branchId);
    Task<Promotion> UpdatePromotionAsync(Promotion promotion);
    Task DeletePromotionAsync(long id);
    Task<bool> HasOverlappingPromotionAsync(long productId, long? branchId, DateTime startDate, DateTime endDate, long? excludePromotionId = null);
    Task<IEnumerable<Promotion>> GetActivePromotionsForBranchAsync(long branchId);
}
