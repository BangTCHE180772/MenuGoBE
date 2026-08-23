namespace MenuGoBE.Interface.Services;

public interface IOrderAssignmentService
{
    /// <summary>
    /// Auto-assign nhân viên cho order nếu chưa có ai assign.
    /// Return true nếu assign thành công, false nếu đã có người khác.
    /// </summary>
    Task<bool> TryAssignAsync(long orderId, long accountId, long branchId);

    /// <summary>
    /// Manager force-assign (xoá assignment cũ nếu có, gán mới).
    /// </summary>
    Task ForceAssignAsync(long orderId, long accountId, long branchId);

    /// <summary>
    /// Lấy accountId đang phụ trách order (null nếu chưa assign).
    /// </summary>
    Task<long?> GetAssignedAccountIdAsync(long orderId);

    /// <summary>
    /// Cập nhật LastActionAt (gọi khi waiter thao tác trên order).
    /// </summary>
    Task TouchAsync(long orderId);

    /// <summary>
    /// Clear assignment (thanh toán / huỷ đơn).
    /// </summary>
    Task ClearAssignmentAsync(long orderId);

    /// <summary>
    /// Clear tất cả assignment của 1 nhân viên (hết ca / disconnect).
    /// </summary>
    Task ClearAllByAccountAsync(long accountId);

    /// <summary>
    /// Lấy danh sách orderIds nhân viên đang phụ trách.
    /// </summary>
    Task<List<long>> GetMyOrderIdsAsync(long accountId);

    /// <summary>
    /// Xử lý merge: xoá assignment bàn con, giữ bàn cha.
    /// </summary>
    Task TransferOnMergeAsync(long childOrderId, long fatherOrderId);
}
