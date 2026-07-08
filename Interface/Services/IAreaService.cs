using MenuGoBE.Dtos.Area;
using MenuGoBE.Dtos.Table;

namespace MenuGoBE.Interface.Services
{
    public interface IAreaService
    {
        Task<List<AreaViewDto>> GetAllAsync();
        Task<AreaViewDto?> GetByIdAsync(long id);
        Task<List<AreaViewDto>> GetByBranchIdAsync(long branchId);

        Task<AreaViewDto> CreateAsync(AreaCreateDto dto);
        Task<bool> UpdateAsync(AreaUpdateDto dto);
        Task<bool> DeleteAsync(long id);

        // Nested: lấy danh sách bàn theo khu vực
        Task<List<TableViewDto>> GetTablesByAreaIdAsync(long areaId);
    }
}
