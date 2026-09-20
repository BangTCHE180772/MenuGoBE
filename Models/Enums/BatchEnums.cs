namespace MenuGoBE.Models.Enums
{
    #region Batch Status Enum
    /// <summary>
    /// Trạng thái hoạt động của Lô hàng (BInventoryBatch).
    /// </summary>
    public enum BatchStatus
    {
        /// <summary>
        /// Lô đang hoạt động, có thể xuất kho theo FEFO.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Lô đã xuất hết (QuantityRemaining = 0).
        /// </summary>
        Depleted = 2,

        /// <summary>
        /// Lô đã hết hạn sử dụng (ExpiryDate < UtcNow). Không được phép FEFO.
        /// </summary>
        Expired = 3,

        /// <summary>
        /// Lô bị khóa tạm thời do kiểm tra chất lượng hoặc tranh chấp.
        /// </summary>
        Locked = 4
    }
    #endregion

    #region Batch Allocation Type Enum
    /// <summary>
    /// Loại phân bổ/tiêu hao Lô hàng trong các nghiệp vụ chứng từ.
    /// </summary>
    public enum BatchAllocationType
    {
        /// <summary>
        /// Bán món ăn tại bàn qua POS hoặc bán trực tiếp.
        /// </summary>
        Sale = 1,

        /// <summary>
        /// Xuất chuyển kho sang chi nhánh khác.
        /// </summary>
        TransferOut = 2,

        /// <summary>
        /// Hoàn trả Lô xuất khi chi nhánh nhận từ chối chuyển kho.
        /// </summary>
        TransferReturn = 3,

        /// <summary>
        /// Tiêu hao nguyên vật liệu để sản xuất thành phẩm.
        /// </summary>
        ProductionConsumption = 4,

        /// <summary>
        /// Xuất hủy hàng hỏng, hàng hết hạn hoặc sự cố.
        /// </summary>
        Disposal = 5,

        /// <summary>
        /// Điều chỉnh số lượng Lô qua quá trình kiểm kê kho.
        /// </summary>
        StockCheck = 6,

        /// <summary>
        /// Xuất trả hàng nhập lại cho Nhà cung cấp.
        /// </summary>
        ReturnToSupplier = 7,

        /// <summary>
        /// Khách hàng trả lại món ăn còn nguyên vẹn qua POS.
        /// </summary>
        CustomerReturn = 8,

        /// <summary>
        /// Nhập Lô thành phẩm từ quy trình sản xuất đồ chuẩn bị sẵn.
        /// </summary>
        ProductionReceipt = 9
    }
    #endregion
}
