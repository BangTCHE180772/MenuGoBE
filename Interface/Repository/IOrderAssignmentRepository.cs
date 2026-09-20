using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IOrderAssignmentRepository
    {
        // === Active assignment queries ===
        Task<OrderAssignment?> GetActiveByOrderIdAsync(long orderId);
        Task<OrderAssignment?> GetActiveByTableIdAsync(long tableId);
        Task<List<OrderAssignment>> GetActiveByAccountIdAsync(long accountId);
        Task<List<OrderAssignment>> GetActiveByBranchAsync(long branchId);
        Task<bool> IsActivelyAssignedAsync(long orderId);

        // === Stale & Orphaned detection ===
        Task<List<OrderAssignment>> GetStaleAssignmentsAsync(int idleMinutes);
        Task<List<OrderAssignment>> GetOrphanedAssignmentsAsync();

        // === CRUD ===
        Task CreateAsync(OrderAssignment assignment);
        Task UpdateAsync(OrderAssignment assignment);

        // === Support ===
        Task CreateSupportAssignmentAsync(long orderId, long accountId, long branchId);

        // === Soft delete ===
        Task DeactivateByOrderIdAsync(long orderId, string reason);
        Task DeactivateByAccountIdAsync(long accountId, string reason);

        // === History (bao gồm cả inactive) ===
        Task<OrderAssignment?> GetByOrderIdAsync(long orderId);
        Task<List<OrderAssignment>> GetByAccountIdAsync(long accountId);

        // === Dependency queries (để Service không cần gọi thẳng DB) ===
        Task<long?> GetTableAreaIdAsync(long tableId);
        Task<List<long>> GetOnDutyStaffIdsAsync(long branchId, DateOnly today);
        Task<bool> IsWaiterAsync(long accountId);

        // === Activity Log ===
        Task<List<OrderAssignment>> GetActivityLogAssignmentsAsync(long accountId, DateTime startTime, DateTime endTime);

        Task SaveChangesAsync();
    }
}
