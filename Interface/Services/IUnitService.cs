using MenuGoBE.Dtos.Unit;

namespace MenuGoBE.Interface.Services
{
    public interface IUnitService
    {
        Task<List<UnitViewDto>> GetAllAsync();
        Task<UnitViewDto?> GetByIdAsync(long id);
        Task<UnitViewDto> CreateAsync(UnitCreateDto dto);
        Task<bool> UpdateAsync(UnitUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
