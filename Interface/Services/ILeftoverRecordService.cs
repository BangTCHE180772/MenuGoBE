using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface ILeftoverRecordService
    {
        Task<List<LeftoverRecordViewDto>> GetByBranchAsync(long branchId, LeftoverType? type, DateOnly? fromDate, DateOnly? toDate);
        Task<LeftoverRecordViewDto> CreateAsync(LeftoverRecordCreateDto dto, long? createdBy);
        Task<bool> DeleteAsync(long id);
    }
}
