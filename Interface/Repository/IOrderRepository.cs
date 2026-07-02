using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(long id);

        Task CreateAsync(Order entity);
        Task UpdateAsync(Order entity);
        Task DeleteAsync(long id);
        Task SaveChangesAsync();
    }
}
