using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Shift;

namespace MenuGoBE.Interface.Services
{
    public interface IShiftService
    {
        Task<List<ShiftViewDto>> GetAllAsync();
        Task<ShiftViewDto?> GetByIdAsync(long id);
        Task<ShiftViewDto> CreateAsync(ShiftCreateDto dto);
        Task<bool> UpdateAsync(ShiftUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
