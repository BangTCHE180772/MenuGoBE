using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.SalaryDetail;

namespace MenuGoBE.Interface.Services
{
    public interface ISalaryDetailService
    {
        Task<List<SalaryDetailViewDto>> GetAllAsync(long? payrollId = null, long? accountId = null);
        Task<SalaryDetailViewDto?> GetByIdAsync(long id);
        Task<SalaryDetailViewDto> CreateAsync(SalaryDetailCreateDto dto);
        Task<bool> UpdateAsync(SalaryDetailUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
