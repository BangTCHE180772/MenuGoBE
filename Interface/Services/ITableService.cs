using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Table;

namespace MenuGoBE.Interface.Services
{
    public interface ITableService
    {
        Task<List<TableViewDto>> GetAllAsync();
        Task<TableViewDto?> GetByIdAsync(long id);

        Task<TableViewDto> CreateAsync(TableCreateDto dto);
        Task<bool> UpdateAsync(TableUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
