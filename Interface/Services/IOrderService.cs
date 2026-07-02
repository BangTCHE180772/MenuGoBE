using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Order;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderService
    {
        Task<List<OrderViewDto>> GetAllAsync();
        Task<OrderViewDto?> GetByIdAsync(long id);

        Task<OrderViewDto> CreateAsync(OrderCreateDto dto);
        Task<bool> UpdateAsync(OrderUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
