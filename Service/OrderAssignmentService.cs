using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class OrderAssignmentService : IOrderAssignmentService
    {
        private readonly IOrderAssignmentRepository _repo;
        private readonly ILogger<OrderAssignmentService> _logger;

        public OrderAssignmentService(
            IOrderAssignmentRepository repo,
            ILogger<OrderAssignmentService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<bool> TryAssignAsync(long orderId, long accountId, long branchId)
        {
            if (await _repo.IsActivelyAssignedAsync(orderId))
                return false;

            // Kiểm tra role của accountId
            if (!await _repo.IsWaiterAsync(accountId))
            {
                _logger.LogInformation("Tài khoản {AccountId} không phải tài khoản của phục vụ. Dừng phân công trực tiếp cho đơn {OrderId}", accountId, orderId);
                return false;
            }

            await _repo.CreateAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = accountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow,
                IsActive = true,
                AssignmentType = "Primary"
            });
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task RecordSupportAsync(long orderId, long accountId, long branchId)
        {
            var primaryAssignment = await _repo.GetActiveByOrderIdAsync(orderId);
            if (primaryAssignment != null && primaryAssignment.AccountId == accountId)
            {
                await TouchAsync(orderId);
                return;
            }

            if (!await _repo.IsWaiterAsync(accountId))
                return;

            await _repo.CreateSupportAssignmentAsync(orderId, accountId, branchId);
            await _repo.SaveChangesAsync();

            _logger.LogInformation(
                "Support recorded: Order {OrderId} <- Support Waiter {AccountId}",
                orderId, accountId);
        }

        public async Task ForceAssignAsync(long orderId, long accountId, long branchId)
        {
            // Deactivate assignment cũ nếu có
            await _repo.DeactivateByOrderIdAsync(orderId, "Reassigned");

            await _repo.CreateAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = accountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow,
                IsActive = true,
                AssignmentType = "Primary"
            });
            await _repo.SaveChangesAsync();
        }

        /// <summary>
        /// Thuật toán Global Least-Loaded + Area Tiebreaker:
        /// 1. Lấy on-duty waiters tại branch
        /// 2. Đếm active assignments mỗi waiter
        /// 3. Sort: ít assignment nhất → ưu tiên cùng khu vực → random
        /// 4. Pick đầu tiên
        /// </summary>
        public async Task<long?> AutoAssignWaiterAsync(long orderId, long tableId, long branchId)
        {
            // Kiểm tra đã assign chưa
            if (await _repo.IsActivelyAssignedAsync(orderId))
            {
                var existing = await _repo.GetActiveByOrderIdAsync(orderId);
                return existing?.AccountId;
            }

            // 1. Lấy AreaId của bàn cần assign
            var targetAreaId = await _repo.GetTableAreaIdAsync(tableId);

            if (targetAreaId == null) return null;

            // 2. Lấy danh sách Waiter đang on-duty tại branch
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var onDutyWaiterIds = await GetOnDutyWaiterIdsAsync(branchId, today);

            if (!onDutyWaiterIds.Any())
            {
                _logger.LogWarning("AutoAssign: No on-duty waiters at branch {BranchId}", branchId);
                return null;
            }

            // 3. Lấy active assignments trong branch
            var allActiveAssignments = await _repo.GetActiveByBranchAsync(branchId);

            // 4. Tính assignment count + area info cho mỗi waiter
            var waiterStats = onDutyWaiterIds.Select(waiterId =>
            {
                var waiterAssignments = allActiveAssignments
                    .Where(a => a.AccountId == waiterId)
                    .ToList();

                var assignmentCount = waiterAssignments.Count;
                var hasTableInSameArea = waiterAssignments
                    .Any(a => a.Order?.Table?.AreaId == targetAreaId);

                return new
                {
                    AccountId = waiterId,
                    AssignmentCount = assignmentCount,
                    HasSameArea = hasTableInSameArea
                };
            }).ToList();

            // 5. Sort: ít assignment → ưu tiên cùng area → random (shuffle trước sort cho stable random)
            var rng = new Random();
            var sorted = waiterStats
                .OrderBy(_ => rng.Next())               // random base
                .OrderByDescending(w => w.HasSameArea)   // cùng area lên đầu (trong cùng count)
                .OrderBy(w => w.AssignmentCount)         // ít nhất lên đầu (primary)
                .ToList();

            var selected = sorted.First();

            // 6. Assign
            await _repo.CreateAsync(new OrderAssignment
            {
                OrderId = orderId,
                AccountId = selected.AccountId,
                BranchId = branchId,
                AssignedAt = DateTime.UtcNow,
                LastActionAt = DateTime.UtcNow,
                IsActive = true,
                AssignmentType = "Primary"
            });
            await _repo.SaveChangesAsync();

            _logger.LogInformation(
                "AutoAssign: Order {OrderId} → Waiter {WaiterId} (count={Count}, sameArea={SameArea})",
                orderId, selected.AccountId, selected.AssignmentCount, selected.HasSameArea);

            return selected.AccountId;
        }

        public async Task<long?> GetAssignedAccountIdAsync(long orderId)
        {
            var assignment = await _repo.GetActiveByOrderIdAsync(orderId);
            return assignment?.AccountId;
        }

        public async Task<long?> GetAssignedAccountByTableAsync(long tableId)
        {
            var assignment = await _repo.GetActiveByTableIdAsync(tableId);
            return assignment?.AccountId;
        }

        public async Task TouchAsync(long orderId)
        {
            var assignment = await _repo.GetActiveByOrderIdAsync(orderId);
            if (assignment != null)
            {
                assignment.LastActionAt = DateTime.UtcNow;
                await _repo.UpdateAsync(assignment);
                await _repo.SaveChangesAsync();
            }
        }

        public async Task DeactivateAssignmentAsync(long orderId, string reason)
        {
            await _repo.DeactivateByOrderIdAsync(orderId, reason);
            await _repo.SaveChangesAsync();
        }

        public async Task DeactivateAllByAccountAsync(long accountId, string reason)
        {
            await _repo.DeactivateByAccountIdAsync(accountId, reason);
            await _repo.SaveChangesAsync();
        }

        public async Task<List<long>> GetMyOrderIdsAsync(long accountId)
        {
            var assignments = await _repo.GetActiveByAccountIdAsync(accountId);
            return assignments.Select(a => a.OrderId).ToList();
        }

        public async Task TransferOnMergeAsync(long childOrderId, long fatherOrderId)
        {
            await _repo.DeactivateByOrderIdAsync(childOrderId, "Merged");
            await _repo.SaveChangesAsync();
        }

        // === Private helpers ===

        /// <summary>
        /// Lấy danh sách AccountId của Waiter đang on-duty tại branch.
        /// Kiểm tra qua WorkSchedule (checked-in, not checked-out) + Role = "Waiter"
        /// </summary>
        private async Task<List<long>> GetOnDutyWaiterIdsAsync(long branchId, DateOnly today)
        {
            // Lấy accountIds đang on-duty tại branch
            var onDutyAccountIds = await _repo.GetOnDutyStaffIdsAsync(branchId, today);

            if (!onDutyAccountIds.Any()) return new List<long>();

            // Lọc chỉ lấy Waiter
            var waiterAccountIds = new List<long>();

            foreach (var accountId in onDutyAccountIds)
            {
                var isWaiter = await _repo.IsWaiterAsync(accountId);
                if (isWaiter)
                {
                    waiterAccountIds.Add(accountId);
                }
            }

            return waiterAccountIds;
        }
    }
}
