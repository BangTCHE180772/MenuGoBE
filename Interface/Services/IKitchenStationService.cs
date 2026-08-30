using MenuGoBE.Dtos.KitchenStation;

namespace MenuGoBE.Interface.Services
{
    public interface IKitchenStationService
    {
        Task<List<KitchenStationViewDto>> GetAllAsync();
        Task<KitchenStationViewDto?> GetByIdAsync(long id);
        Task<List<KitchenStationViewDto>> GetByBranchIdAsync(long branchId);

        Task<KitchenStationViewDto> CreateAsync(KitchenStationCreateDto dto);
        Task<bool> UpdateAsync(KitchenStationUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
