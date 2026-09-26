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
                .AsNoTracking()
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ChildOrders)
                    .ThenInclude(c => c.Table)
                        .ThenInclude(t => t.Area)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(long id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<List<Order>> GetChildOrdersAsync(long fatherId)
        {
            return await _context.Orders
                .AsNoTracking()
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

        public async Task<List<Order>> GetActiveKitchenOrdersWithDetailsAsync(long? branchId = null)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Active" || o.Status == "Internal");

            if (branchId.HasValue)
            {
                query = query.Where(o => o.Table.Area.BranchId == branchId.Value);
            }

            return await query
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
                        .ThenInclude(a => a.Branch)
                            .ThenInclude(b => b.Address)
                                .ThenInclude(addr => addr.NewWard)
                                    .ThenInclude(w => w.NewProvince)
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                        .ThenInclude(a => a.Branch)
                            .ThenInclude(b => b.Address)
                                .ThenInclude(addr => addr.OldWard)
                                    .ThenInclude(w => w.OldDistrict)
                                        .ThenInclude(d => d.OldProvince)
                .Include(o => o.Customer)
                .Include(o => o.Voucher)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync();
        }

        #region Lấy danh sách hóa đơn đã thanh toán theo chi nhánh và khoảng thời gian
        public async Task<List<Order>> GetPaidOrdersByBranchAsync(long branchId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => (branchId <= 0 || (o.Table != null && o.Table.Area != null && o.Table.Area.BranchId == branchId)) 
                         && o.Status == "Paid" 
                         && o.FatherId == null
                         && !(o.TotalAmount == 0 
                              && !o.OrderDetails.Any(od => (od.Quantity - od.ReturnedQuantity) > 0) 
                              && !o.ChildOrders.Any(c => c.OrderDetails.Any(od => (od.Quantity - od.ReturnedQuantity) > 0))));

            // Lọc theo mốc thời gian bắt đầu (UTC+7)
            if (startDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(startDate.Value.Date.AddHours(-7), DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= startUtc);
            }

            // Lọc theo mốc thời gian kết thúc (UTC+7)
            if (endDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddHours(-7).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt <= endUtc);
            }

            return await query
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .Include(o => o.ChildOrders)
                    .ThenInclude(c => c.Table)
                .Include(o => o.Customer)
                .Include(o => o.Voucher)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        #endregion

        public async Task<List<Order>> GetReturnableOrdersByBranchAsync(long branchId)
        {
            var validOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Table != null && o.Table.Area != null && o.Table.Area.BranchId == branchId && o.Status != "Paid" && o.Status != "Completed" && o.Status != "Cancelled" && o.Status != "Internal")
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .Include(o => o.OrderDetails.Where(od => od.Status != "Cancelled" && (od.CookingStatus == "Served" || od.CookingStatus == null || od.Status == "PartialReturned") && od.Quantity > od.ReturnedQuantity))
                    .ThenInclude(od => od.Product)
                .Where(o => o.OrderDetails.Any(od => od.Status != "Cancelled" && (od.CookingStatus == "Served" || od.CookingStatus == null || od.Status == "PartialReturned") && od.Quantity > od.ReturnedQuantity))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return validOrders;
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

        public Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy()
        {
            return _context.Database.CreateExecutionStrategy();
        }

        // Sau khi rollback transaction, EF vẫn giữ các thay đổi chưa lưu trong change tracker —
        // gọi hàm này để món lỗi không "dính" sang lần SaveChanges của món kế tiếp.
        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }
    }
}
