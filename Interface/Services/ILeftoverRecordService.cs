using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface ILeftoverRecordService
    {
        Task<List<LeftoverRecordViewDto>> GetByBranchAsync(long branchId, LeftoverType? type, DateOnly? fromDate, DateOnly? toDate);
        Task<List<LeftoverReusableDto>> GetReusableAsync(long branchId, long? productId);
        Task<LeftoverRecordViewDto> CreateAsync(LeftoverRecordCreateDto dto, long? createdBy);
        Task MarkUsedAsync(long id, long productId, int quantity, long orderDetailId);
        Task<bool> DeleteAsync(long id);
    }
}
