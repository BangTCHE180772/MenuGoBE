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
            return await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Payments)
                .Include(o => o.ChildOrders)
                    .ThenInclude(c => c.Table)
                .ToListAsync();
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
                .Where(o => o.FatherId == fatherId && (o.Status == "Active" || o.Status == "Reserved"))
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                .ToListAsync();
        }

        public async Task<Order?> GetActiveOrderByTableIdAsync(long tableId)
        {
            var activeOrders = await _context.Orders
                .Where(o => o.TableId == tableId && (o.Status == "Active" || o.Status == "Internal"))
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .ToListAsync();

            return activeOrders
                .OrderBy(o => o.Status == "Active" ? 0 : 1)
                .ThenBy(o => o.CreatedAt)
                .FirstOrDefault();
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

        public async Task<List<Order>> GetActiveKitchenOrdersWithDetailsAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == "Active" || o.Status == "Internal")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Image)
                .Include(o => o.Table)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdWithDetailsAsync(long orderId)
        {
            return await _context.Orders
                .Where(o => o.Id == orderId)
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

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
