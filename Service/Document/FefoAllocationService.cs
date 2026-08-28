using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Service.Document
{
    /// <summary>
    /// Triển khai FEFO Engine, Quản lý Concurrency và Đối soát Lô hàng.
    /// </summary>
    public class FefoAllocationService : IFefoAllocationService
    {
        #region Khai báo Dependencies & Constructor
        private readonly AppDbContext _context;
        private readonly ILogger<FefoAllocationService> _logger;

        public FefoAllocationService(AppDbContext context, ILogger<FefoAllocationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        #endregion

        #region Khóa dòng dữ liệu PostgreSQL (Row Locking FOR UPDATE)
        /// <summary>
        /// Khóa bản ghi BInventory trong PostgreSQL bằng SELECT ... FOR UPDATE để chống race condition.
        /// </summary>
        public async Task<BInventory?> LockBInventoryForUpdateAsync(long bInventoryId, CancellationToken cancellationToken = default)
        {
            // Sử dụng Raw SQL FOR UPDATE của PostgreSQL để giữ row-level exclusive lock
            var lockedInventories = await _context.BInventories
                .FromSqlInterpolated($"SELECT * FROM \"BInventories\" WHERE \"Id\" = {bInventoryId} FOR UPDATE")
                .Include(b => b.Product)
                .ToListAsync(cancellationToken);

            return lockedInventories.FirstOrDefault();
        }
        #endregion

        #region Thuật toán Cấp phát Lô hàng FEFO (AllocateAsync)
        /// <summary>
        /// Thực hiện phân bổ xuất kho từ các Lô hàng Active theo nguyên tắc FEFO:
        /// 1. ExpiryDate gần nhất lên trước (ExpiryDate ASC)
        /// 2. Lô có ExpiryDate == NULL xếp sau cùng
        /// 3. Ngày nhận sớm nhất lên trước (ReceivedDate ASC)
        /// 4. Id ASC (deterministic tie-breaker)
        /// </summary>
        public async Task<IReadOnlyList<FefoAllocationResult>> AllocateAsync(
            long bInventoryId,
            decimal requiredQuantity,
            CancellationToken cancellationToken = default)
        {
            // 1. Validate số lượng yêu cầu hợp lệ
            if (requiredQuantity <= 0)
            {
                throw new ArgumentException($"Số lượng yêu cầu cấp phát FEFO phải lớn hơn 0. Giá trị nhận được: {requiredQuantity}", nameof(requiredQuantity));
            }

            var nowUtc = DateTime.UtcNow;

            // 2. Truy vấn toàn bộ các Lô hàng Active, còn tồn và chưa hết hạn của BInventory này
            var candidateBatches = await _context.BInventoryBatches
                .Where(b => b.BInventoryId == bInventoryId
                            && b.Status == BatchStatus.Active
                            && b.QuantityRemaining > 0
                            && (b.ExpiryDate == null || b.ExpiryDate > nowUtc))
                .ToListAsync(cancellationToken);

            // 3. Sắp xếp theo chuẩn thứ tự ưu tiên FEFO
            var sortedBatches = candidateBatches
                .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                .ThenBy(b => b.ExpiryDate)
                .ThenBy(b => b.ReceivedDate)
                .ThenBy(b => b.Id)
                .ToList();

            // 4. Kiểm tra tổng số lượng khả dụng
            decimal totalAvailable = sortedBatches.Sum(b => b.QuantityRemaining);
            if (totalAvailable < requiredQuantity)
            {
                _logger.LogWarning($"[FEFO] Không đủ tồn kho khả dụng cho BInventoryId={bInventoryId}. Cần: {requiredQuantity:N3}, Khả dụng: {totalAvailable:N3}");
                throw new MenuGoException(
                    ErrorCodes.DocumentInsufficientInventory,
                    $"Tồn kho khả dụng của mặt hàng không đủ để phân bổ. Yêu cầu: {requiredQuantity:N3}, Khả dụng: {totalAvailable:N3}.");
            }

            // 5. Tiến hành phân bổ tuần tự từng Lô
            var results = new List<FefoAllocationResult>();
            decimal remainingRequired = requiredQuantity;

            foreach (var batch in sortedBatches)
            {
                if (remainingRequired <= 0) break;

                decimal allocateFromThisBatch = Math.Min(batch.QuantityRemaining, remainingRequired);

                // Giảm số lượng tồn trên Lô
                batch.QuantityRemaining -= allocateFromThisBatch;
                batch.UpdatedAt = nowUtc;

                // Nếu Lô đã xuất hết -> chuyển sang trạng thái Depleted
                if (batch.QuantityRemaining == 0)
                {
                    batch.Status = BatchStatus.Depleted;
                }

                results.Add(new FefoAllocationResult
                {
                    BatchId = batch.Id,
                    BatchCode = batch.BatchCode,
                    QuantityAllocated = allocateFromThisBatch,
                    UnitCost = batch.UnitCost,
                    ExpiryDate = batch.ExpiryDate,
                    ReceivedDate = batch.ReceivedDate
                });

                remainingRequired -= allocateFromThisBatch;
            }

            return results;
        }
        #endregion

        #region Kiểm tra Đối soát Tính Toàn vẹn (CheckReconciliationAsync)
        /// <summary>
        /// Kiểm tra sự khớp tuyệt đối giữa BInventory.Quantity và SUM(Batch.QuantityRemaining).
        /// </summary>
        public async Task<ReconciliationResult> CheckReconciliationAsync(long bInventoryId, CancellationToken cancellationToken = default)
        {
            var inventory = await _context.BInventories
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bInventoryId, cancellationToken);

            if (inventory == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy bản ghi BInventory với ID {bInventoryId}.");
            }

            // Lấy tất cả các Lô vật lý đang có trong kho (Active và Expired)
            var batches = await _context.BInventoryBatches
                .AsNoTracking()
                .Where(b => b.BInventoryId == bInventoryId && b.QuantityRemaining > 0)
                .ToListAsync(cancellationToken);

            decimal totalBatchQuantity = batches.Sum(b => b.QuantityRemaining);

            var result = new ReconciliationResult
            {
                BInventoryId = bInventoryId,
                BInventoryQuantity = inventory.Quantity,
                TotalBatchQuantity = totalBatchQuantity,
                BatchesCount = batches.Count
            };

            if (!result.IsConsistent)
            {
                _logger.LogError($"[RECONCILIATION DRIFT] BInventoryId={bInventoryId} bị lệch tồn kho! BInventory={inventory.Quantity:N3}, TotalBatches={totalBatchQuantity:N3}, Drift={result.DriftQuantity:N3}");
            }

            return result;
        }
        #endregion
    }
}
