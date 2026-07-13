using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(long id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<List<Order>> GetChildOrdersAsync(long fatherId)
        {
            return await _context.Orders
                .Where(o => o.FatherId == fatherId)
                .ToListAsync();
        }

        public async Task<List<Order>> GetChildOrdersWithDetailsAsync(long fatherId)
        {
            return await _context.Orders
                .Where(o => o.FatherId == fatherId && o.Status == "Active")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                .ToListAsync();
        }

        public async Task<Order?> GetActiveOrderByTableIdAsync(long tableId)
        {
            return await _context.Orders
                .Where(o => o.TableId == tableId && o.Status == "Active")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Order?> GetActiveOrderByIdWithDetailsAsync(long orderId)
        {
            return await _context.Orders
                .Where(o => o.Id == orderId && o.Status == "Active")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Order entity)
        {
            await _context.Orders.AddAsync(entity);
        }

        public Task UpdateAsync(Order entity)
        {
            _context.Orders.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.Orders.FindAsync(id);
            if (entity != null)
            {
                _context.Orders.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
