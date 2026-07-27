using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly AppDbContext _context;

        public OrderDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDetail>> GetByOrderIdAsync(long orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderDetail?> GetByIdAsync(long id)
        {
            return await _context.OrderDetails.FindAsync(id);
        }

        public async Task CreateAsync(OrderDetail entity)
        {
            await _context.OrderDetails.AddAsync(entity);
        }

        public Task UpdateAsync(OrderDetail entity)
        {
            _context.OrderDetails.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity != null)
            {
                _context.OrderDetails.Remove(entity);
            }
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity == null) return false;

            entity.Status = status;
            if (status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
            {
                entity.CookingStatus = "Waiting";
            }
            return true;
        }

        public async Task<bool> UpdateCookingStatusAsync(long id, string cookingStatus)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity == null) return false;

            entity.CookingStatus = cookingStatus;
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
