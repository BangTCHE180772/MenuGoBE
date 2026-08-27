using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class OrderAssignmentService : IOrderAssignmentService
    {
        private readonly IOrderAssignmentRepository _repo;

        public OrderAssignmentService(IOrderAssignmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> TryAssignAsync(long orderId, long accountId, long branchId)
        {
            if (await _repo.IsAssignedAsync(orderId))
                return false;

            await _repo.CreateAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = accountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow
            });
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task ForceAssignAsync(long orderId, long accountId, long branchId)
        {
            await _repo.DeleteByOrderIdAsync(orderId);

            await _repo.CreateAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = accountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow
            });
            await _repo.SaveChangesAsync();
        }

        public async Task<long?> GetAssignedAccountIdAsync(long orderId)
        {
            var assignment = await _repo.GetByOrderIdAsync(orderId);
            return assignment?.AccountId;
        }

        public async Task TouchAsync(long orderId)
        {
            var assignment = await _repo.GetByOrderIdAsync(orderId);
            if (assignment != null)
            {
                assignment.LastActionAt = DateTime.UtcNow;
                await _repo.UpdateAsync(assignment);
                await _repo.SaveChangesAsync();
            }
        }

        public async Task ClearAssignmentAsync(long orderId)
        {
            await _repo.DeleteByOrderIdAsync(orderId);
            await _repo.SaveChangesAsync();
        }

        public async Task ClearAllByAccountAsync(long accountId)
        {
            await _repo.DeleteByAccountIdAsync(accountId);
            await _repo.SaveChangesAsync();
        }

        public async Task<List<long>> GetMyOrderIdsAsync(long accountId)
        {
            var assignments = await _repo.GetByAccountIdAsync(accountId);
            return assignments.Select(a => a.OrderId).ToList();
        }

        public async Task TransferOnMergeAsync(long childOrderId, long fatherOrderId)
        {
            await _repo.DeleteByOrderIdAsync(childOrderId);
            await _repo.SaveChangesAsync();
        }
    }
}
