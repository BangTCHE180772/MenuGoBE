using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Table;

namespace MenuGoBE.Interface.Services
{
    public interface ITableService
    {
        Task<List<TableViewDto>> GetAllAsync(bool includeInternal = false);
        Task<List<TableViewDto>> GetByBranchIdsAsync(List<long> branchIds, bool includeInternal = false);
        Task<TableViewDto?> GetByIdAsync(long id);

        Task<TableViewDto> CreateAsync(TableCreateDto dto);
        Task<bool> UpdateAsync(TableUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
