using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Order
{
    public class OrderViewDto
    {
        public long Id { get; set; }

        public string OrderCode => $"HD{Id}";

        public long TableId { get; set; }

        public long? FatherId { get; set; }

        public string Status { get; set; } = string.Empty;

        public long? CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public long? VoucherId { get; set; }

        public string? VoucherCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public int PointsUsed { get; set; }

        public decimal TotalAmount { get; set; }

        public long? CreatedBy { get; set; }

        public string? CreatedByName { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? TableName { get; set; }

        public string? AreaName { get; set; }

        public long? BranchId { get; set; }

        public string? BranchName { get; set; }

        public string? BranchAddress { get; set; }

        public string? PaymentMethod { get; set; }

        public List<OrderDetailViewDto> OrderDetails { get; set; } = new List<OrderDetailViewDto>();
    }
}
