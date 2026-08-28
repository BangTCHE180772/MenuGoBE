using System;

namespace MenuGoBE.Dtos.Document
{
    #region Kết quả phân bổ FEFO (FefoAllocationResult)
    /// <summary>
    /// Chứa thông tin chi tiết từng Lô hàng được phân bổ bởi FEFO Engine.
    /// </summary>
    public sealed class FefoAllocationResult
    {
        public long BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public decimal QuantityAllocated { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; }
    }
    #endregion

    #region Kết quả đối soát tồn kho (ReconciliationResult)
    /// <summary>
    /// Chứa kết quả kiểm tra sự nhất quán giữa BInventory.Quantity và SUM(Batch.QuantityRemaining).
    /// </summary>
    public sealed class ReconciliationResult
    {
        public long BInventoryId { get; set; }
        public decimal BInventoryQuantity { get; set; }
        public decimal TotalBatchQuantity { get; set; }
        public decimal DriftQuantity => BInventoryQuantity - TotalBatchQuantity;
        public bool IsConsistent => Math.Abs(DriftQuantity) < 0.0001m;
        public int BatchesCount { get; set; }
    }
    #endregion
}
