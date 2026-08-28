using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services.Document
{
    #region Interface Dịch vụ Phân bổ FEFO (IFefoAllocationService)
    /// <summary>
    /// Định nghĩa các thao tác phân bổ Lô hàng theo nguyên tắc FEFO (First-Expired, First-Out),
    /// khóa dòng Concurrency và đối soát tính toàn vẹn tồn kho.
    /// </summary>
    public interface IFefoAllocationService
    {
        /// <summary>
        /// Phân bổ số lượng cần xuất từ các Lô hàng khả dụng theo thứ tự FEFO:
        /// 1. ExpiryDate ASC (Lô có HSD gần nhất trước)
        /// 2. ExpiryDate == NULL (Lô không có HSD sau cùng)
        /// 3. ReceivedDate ASC (Lô nhập trước trước)
        /// 4. Id ASC (Deterministic tie-breaker)
        /// </summary>
        /// <param name="bInventoryId">ID bản ghi tồn kho BInventory</param>
        /// <param name="requiredQuantity">Số lượng cơ sở cần xuất</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Danh sách phân bổ chi tiết theo từng Lô</returns>
        Task<IReadOnlyList<FefoAllocationResult>> AllocateAsync(
            long bInventoryId,
            decimal requiredQuantity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Khóa dòng dữ liệu BInventory bằng PostgreSQL `SELECT ... FOR UPDATE` để ngăn chặn race condition.
        /// Bắt buộc phải được gọi bên trong một Transaction còn hiệu lực.
        /// </summary>
        /// <param name="bInventoryId">ID bản ghi BInventory cần khóa</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Bản ghi BInventory đã được khóa</returns>
        Task<BInventory?> LockBInventoryForUpdateAsync(
            long bInventoryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đối soát kiểm tra tính toàn vẹn bất biến: BInventory.Quantity == SUM(Batch.QuantityRemaining).
        /// Không tự ý sửa dữ liệu khi phát hiện sai lệch.
        /// </summary>
        /// <param name="bInventoryId">ID bản ghi BInventory cần kiểm tra</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Kết quả đối soát kèm độ lệch Drift nếu có</returns>
        Task<ReconciliationResult> CheckReconciliationAsync(
            long bInventoryId,
            CancellationToken cancellationToken = default);
    }
    #endregion
}
