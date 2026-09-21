using System;

namespace MenuGoBE.Dtos.Document
{
    #region DTO Thiết lập cảnh báo tồn kho chi nhánh
    public class StockAlertSettingsDto
    {
        public long BranchId { get; set; }
        public decimal CriticalThreshold { get; set; } = 20;
        public decimal WarningThreshold { get; set; } = 40;
    }
    #endregion

    #region DTO Thiết lập cảnh báo tồn kho riêng cho từng mặt hàng
    public class ItemStockAlertSettingsDto
    {
        public long BInventoryId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal? CustomCriticalThreshold { get; set; }
        public decimal? CustomWarningThreshold { get; set; }
        public decimal BranchCriticalThreshold { get; set; }
        public decimal BranchWarningThreshold { get; set; }
        public bool IsAlertEnabled { get; set; }
        public bool HasCustomSettings => CustomCriticalThreshold.HasValue || CustomWarningThreshold.HasValue;
    }

    public class UpdateItemStockAlertSettingsDto
    {
        public decimal? CustomCriticalThreshold { get; set; }
        public decimal? CustomWarningThreshold { get; set; }
        public bool IsAlertEnabled { get; set; }
    }
    #endregion
}
