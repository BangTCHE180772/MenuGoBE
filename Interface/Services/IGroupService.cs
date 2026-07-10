using MenuGoBE.Dtos.Group;

namespace MenuGoBE.Interface.Services
{
    public interface IGroupService
    {
        Task<List<GroupViewDto>> GetAllAsync();
        Task<GroupViewDto?> GetByIdAsync(long id);
        Task<GroupViewDto> CreateAsync(GroupCreateDto dto);
        Task<bool> UpdateAsync(GroupUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
