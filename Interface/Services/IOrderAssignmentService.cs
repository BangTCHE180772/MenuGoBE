using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface IOrderAssignmentService
    {
        /// <summary>
        /// Waiter tự nhận bàn. Trả false nếu đã có người assign.
        /// </summary>
        Task<bool> TryAssignAsync(long orderId, long accountId, long branchId);

        /// <summary>
        /// Ghi nhận hành động hỗ trợ của nhân viên.
        /// </summary>
        Task RecordSupportAsync(long orderId, long accountId, long branchId);

        /// <summary>
        /// Manager chỉ định bắt buộc. Deactivate assignment cũ nếu có.
        /// </summary>
        Task ForceAssignAsync(long orderId, long accountId, long branchId);

        /// <summary>
        /// Tự động assign waiter theo thuật toán Global Least-Loaded + Area Tiebreaker.
        /// Dùng khi khách tự order (CustomerPending) hoặc reassign sau orphaned.
        /// Trả về AccountId của waiter được assign, null nếu không tìm được.
        /// </summary>
        Task<long?> AutoAssignWaiterAsync(long orderId, long tableId, long branchId);

        /// <summary>
        /// Lấy AccountId của waiter đang phụ trách order (active assignment).
        /// </summary>
        Task<long?> GetAssignedAccountIdAsync(long orderId);

        /// <summary>
        /// Lấy AccountId theo TableId (cho CustomerAction targeted notification).
        /// </summary>
        Task<long?> GetAssignedAccountByTableAsync(long tableId);

        /// <summary>
        /// Cập nhật LastActionAt khi waiter thao tác trên order.
        /// </summary>
        Task TouchAsync(long orderId);

        /// <summary>
        /// Soft-delete assignment. Lý do: "Paid", "Cancelled", "Reassigned", "Merged", "ShiftEnded", "ManualUnassign"
        /// </summary>
        Task DeactivateAssignmentAsync(long orderId, string reason);

        /// <summary>
        /// Deactivate tất cả assignment active của 1 account.
        /// </summary>
        Task DeactivateAllByAccountAsync(long accountId, string reason);

        /// <summary>
        /// Lấy danh sách orderId đang active assign cho 1 account.
        /// </summary>
        Task<List<long>> GetMyOrderIdsAsync(long accountId);

        /// <summary>
        /// Xử lý merge: deactivate assignment bàn con.
        /// </summary>
        Task TransferOnMergeAsync(long childOrderId, long fatherOrderId);
    }
}
