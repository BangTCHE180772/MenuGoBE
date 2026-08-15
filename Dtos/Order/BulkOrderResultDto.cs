using System.Collections.Generic;

namespace MenuGoBE.Dtos.Order
{
    public class InsufficientStockDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class BulkOrderResultDto
    {
        public bool Success { get; set; }
        public List<InsufficientStockDto> Errors { get; set; } = new List<InsufficientStockDto>();
        public List<OrderDetailViewDto> Details { get; set; } = new List<OrderDetailViewDto>();
    }
}
