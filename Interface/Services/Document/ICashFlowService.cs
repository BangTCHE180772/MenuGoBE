using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface ICashFlowService
    {
        Task<IEnumerable<CashFlowResponseDto>> GetCashFlowsAsync(long? branchId);

        Task<CashFlowResponseDto?> GetByIdAsync(long id);

        Task<CashFlowResponseDto> CreateAsync(CreateCashFlowDto dto, long userId);

        Task<bool> SoftDeleteAsync(long id, string? deleteNote, long userId);
    }
}
