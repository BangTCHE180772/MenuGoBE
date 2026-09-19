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

        // === Active assignment queries ===

        public async Task<OrderAssignment?> GetActiveByOrderIdAsync(long orderId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                        .ThenInclude(t => t.Area)
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.IsActive);
        }

        public async Task<OrderAssignment?> GetActiveByTableIdAsync(long tableId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                .FirstOrDefaultAsync(a =>
                    a.Order.TableId == tableId &&
                    a.Order.Status == "Active" &&
                    a.IsActive);
        }

        public async Task<List<OrderAssignment>> GetActiveByAccountIdAsync(long accountId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                        .ThenInclude(t => t.Area)
                .Where(a => a.AccountId == accountId && a.IsActive)
                .ToListAsync();
        }

        public async Task<List<OrderAssignment>> GetActiveByBranchAsync(long branchId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                        .ThenInclude(t => t.Area)
                .Where(a => a.BranchId == branchId && a.IsActive)
                .ToListAsync();
        }

        public async Task<bool> IsActivelyAssignedAsync(long orderId)
        {
            return await _context.OrderAssignments
                .AnyAsync(a => a.OrderId == orderId && a.IsActive);
        }

        // === Stale & Orphaned detection ===

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
                    a.IsActive &&
                    a.LastActionAt <= threshold &&
                    a.Order.Status == "Active" &&
                    a.Order.OrderDetails.Any(od =>
                        od.CookingStatus == "Ready" &&
                        od.Status == "Confirmed"))
                .ToListAsync();
        }

        public async Task<List<OrderAssignment>> GetOrphanedAssignmentsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Include(a => a.Account)
                .Where(a =>
                    a.IsActive &&
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

        // === CRUD ===

        public async Task CreateAsync(OrderAssignment assignment)
        {
            await _context.OrderAssignments.AddAsync(assignment);
        }

        public Task UpdateAsync(OrderAssignment assignment)
        {
            _context.OrderAssignments.Update(assignment);
            return Task.CompletedTask;
        }

        public async Task CreateSupportAssignmentAsync(long orderId, long accountId, long branchId)
        {
            await _context.OrderAssignments.AddAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = accountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow,
                IsActive = false,
                AssignmentType = "Support",
                DeactivatedReason = "SupportAssist",
                DeactivatedAt = DateTime.UtcNow
            });
        }

        // === Soft delete ===

        public async Task DeactivateByOrderIdAsync(long orderId, string reason)
        {
            var assignment = await _context.OrderAssignments
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.IsActive);
            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.DeactivatedReason = reason;
                assignment.DeactivatedAt = DateTime.UtcNow;
            }
        }

        public async Task DeactivateByAccountIdAsync(long accountId, string reason)
        {
            var assignments = await _context.OrderAssignments
                .Where(a => a.AccountId == accountId && a.IsActive)
                .ToListAsync();
            foreach (var a in assignments)
            {
                a.IsActive = false;
                a.DeactivatedReason = reason;
                a.DeactivatedAt = DateTime.UtcNow;
            }
        }

        // === History ===

        public async Task<OrderAssignment?> GetByOrderIdAsync(long orderId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Account)
                .Include(a => a.Order)
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync(a => a.OrderId == orderId);
        }

        public async Task<List<OrderAssignment>> GetByAccountIdAsync(long accountId)
        {
            return await _context.OrderAssignments
                .Include(a => a.Order)
                    .ThenInclude(o => o.Table)
                .Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        // === Dependency queries ===

        public async Task<long?> GetTableAreaIdAsync(long tableId)
        {
            var table = await _context.Tables
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.Id == tableId);
            return table?.AreaId;
        }

        public async Task<List<long>> GetOnDutyStaffIdsAsync(long branchId, DateOnly today)
        {
            return await _context.WorkSchedules
                .Where(ws =>
                    ws.BranchId == branchId &&
                    ws.WorkDate == today &&
                    ws.CheckInAt != null &&
                    ws.CheckOutAt == null &&
                    ws.Status == "Working")
                .Select(ws => ws.AccountId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> IsWaiterAsync(long accountId)
        {
            var now = DateTime.UtcNow;

            // Check qua Contract
            var hasWaiterContract = await _context.Contracts
                .Include(c => c.Role)
                .AnyAsync(c =>
                    c.AccountId == accountId &&
                    c.Status == "Active" &&
                    c.Role.Name.ToLower() == "waiter");

            if (hasWaiterContract) return true;

            // Check qua TempRole
            var hasWaiterTempRole = await _context.Set<TempRole>()
                .Include(tr => tr.Role)
                .AnyAsync(tr =>
                    tr.AccountId == accountId &&
                    tr.Status == "Active" &&
                    tr.StartTime <= now &&
                    tr.EndTime >= now &&
                    tr.Role.Name.ToLower() == "waiter");

            return hasWaiterTempRole;
        }

        public async Task<List<OrderAssignment>> GetActivityLogAssignmentsAsync(long accountId, DateTime startTime, DateTime endTime)
        {
            return await _context.OrderAssignments
                .Include(a => a.Order)
                .ThenInclude(o => o.Table)
                .Where(a => a.AccountId == accountId && a.AssignedAt >= startTime && a.AssignedAt <= endTime)
                .OrderBy(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
