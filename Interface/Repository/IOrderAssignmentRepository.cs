using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IOrderAssignmentRepository
{
    /// <summary>Lấy assignment của order (null nếu chưa assign)</summary>
    Task<OrderAssignment?> GetByOrderIdAsync(long orderId);

    /// <summary>Lấy danh sách assignments của nhân viên</summary>
    Task<List<OrderAssignment>> GetByAccountIdAsync(long accountId);

    /// <summary>Lấy danh sách assignments theo branch</summary>
    Task<List<OrderAssignment>> GetByBranchIdAsync(long branchId);

    /// <summary>Kiểm tra order đã có assignment chưa</summary>
    Task<bool> IsAssignedAsync(long orderId);

    /// <summary>Tạo assignment mới</summary>
    Task CreateAsync(OrderAssignment assignment);

    /// <summary>Cập nhật assignment</summary>
    Task UpdateAsync(OrderAssignment assignment);

    /// <summary>Xoá assignment theo orderId</summary>
    Task DeleteByOrderIdAsync(long orderId);

    /// <summary>Xoá tất cả assignments của 1 nhân viên</summary>
    Task DeleteByAccountIdAsync(long accountId);

    /// <summary>
    /// Lấy assignments có món Ready nhưng waiter idle quá X phút.
    /// Dùng cho stale assignment detection.
    /// </summary>
    Task<List<OrderAssignment>> GetStaleAssignmentsAsync(int idleMinutes);

    /// <summary>
    /// Lấy assignments mà waiter đã checkout / không còn on-duty.
    /// Dùng cho orphaned assignment detection.
    /// </summary>
    Task<List<OrderAssignment>> GetOrphanedAssignmentsAsync();

    Task SaveChangesAsync();
}
