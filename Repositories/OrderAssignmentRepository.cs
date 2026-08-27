using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OrderAssignmentRepository : IOrderAssignmentRepository
    {
        private readonly AppDbContext _context;

        public OrderAssignmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderAssignment?> GetByOrderIdAsync(long orderId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .FirstOrDefaultAsync(a => a.OrderId == orderId);
        }

        public async Task<List<OrderAssignment>> GetByAccountIdAsync(long accountId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Where(a => a.AccountId == accountId)
                .ToListAsync();
        }

        public async Task<List<OrderAssignment>> GetByBranchIdAsync(long branchId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Where(a => a.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<bool> IsAssignedAsync(long orderId)
        {
            return await _context.OrderAssignments.AnyAsync(a => a.OrderId == orderId);
        }

        public async Task CreateAsync(OrderAssignment assignment)
        {
            await _context.OrderAssignments.AddAsync(assignment);
        }

        public Task UpdateAsync(OrderAssignment assignment)
        {
            _context.OrderAssignments.Update(assignment);
            return Task.CompletedTask;
        }

        public async Task DeleteByOrderIdAsync(long orderId)
        {
            var assignment = await _context.OrderAssignments
                .FirstOrDefaultAsync(a => a.OrderId == orderId);
            if (assignment != null)
            {
                _context.OrderAssignments.Remove(assignment);
            }
        }

        public async Task DeleteByAccountIdAsync(long accountId)
        {
            var assignments = await _context.OrderAssignments
                .Where(a => a.AccountId == accountId)
                .ToListAsync();
            _context.OrderAssignments.RemoveRange(assignments);
        }

        public async Task<List<OrderAssignment>> GetStaleAssignmentsAsync(int idleMinutes)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-idleMinutes);

            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.OrderDetails)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Include(a => a.Account)
                .Where(a =>
                    a.LastActionAt <= threshold &&
                    a.Order.Status == "Active" &&
                    a.Order.OrderDetails.Any(od =>
                        od.CookingStatus == "Ready" &&
                        od.Status == "Confirmed"))
                .ToListAsync();
        }

        public async Task<List<OrderAssignment>> GetOrphanedAssignmentsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // Vietnam timezone

            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Include(a => a.Account)
                .Where(a =>
                    a.Order.Status == "Active" &&
                    !_context.WorkSchedules.Any(ws =>
                        ws.AccountId == a.AccountId &&
                        ws.BranchId == a.BranchId &&
                        ws.WorkDate == today &&
                        ws.CheckInAt != null &&
                        ws.CheckOutAt == null &&
                        ws.Status == "Working"))
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
