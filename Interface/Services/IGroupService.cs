using MenuGoBE.Dtos.Group;

namespace MenuGoBE.Interface.Services
{
    public interface IGroupService
    {
        Task<List<GroupViewDto>> GetAllAsync();
        Task<List<GroupViewDto>> GetSellableAsync(long? branchId = null);
        Task<GroupViewDto?> GetByIdAsync(long id);
        Task<GroupViewDto> CreateAsync(GroupCreateDto dto);
        Task<bool> UpdateAsync(GroupUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
