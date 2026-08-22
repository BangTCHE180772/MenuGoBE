using MenuGoBE.Dtos.HolidayConfig;

namespace MenuGoBE.Interface.Services;

public interface IHolidayConfigService
{
    Task<List<HolidayConfigViewDto>> GetAllAsync(long? branchId = null);
    Task<HolidayConfigViewDto?> GetByIdAsync(long id);
    Task<HolidayConfigViewDto> CreateAsync(HolidayConfigCreateDto dto);
    Task<bool> UpdateAsync(HolidayConfigUpdateDto dto);
    Task<bool> DeleteAsync(long id);
}
