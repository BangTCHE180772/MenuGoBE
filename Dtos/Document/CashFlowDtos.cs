using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO tạo phiếu thu chi thủ công.
    /// </summary>
    public class CreateCashFlowDto
    {
        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        /// <summary>
        /// Hướng dòng tiền: 1 = Thu (Inflow), 2 = Chi (Outflow)
        /// </summary>
        [Range(1, 2, ErrorMessage = "Direction chỉ được nhận giá trị 1 (Thu) hoặc 2 (Chi).")]
        public int Direction { get; set; }

        [Range(0.01, 10000000000.0, ErrorMessage = "Số tiền phải lớn hơn 0 và không vượt quá 10 tỷ VNĐ.")]
        public decimal TotalAmount { get; set; }

        public long? PartnerId { get; set; }

        /// <summary>
        /// Phương thức thanh toán: 1 = Tiền mặt, 2 = Thẻ, 3 = Chuyển khoản
        /// </summary>
        public int PaymentMethod { get; set; } = 1;

        public DateTime BusinessDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO chi tiết phản hồi thông tin Phiếu Thu Chi.
    /// </summary>
    public class CashFlowResponseDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public long BranchId { get; set; }
        public long? DocumentId { get; set; }
        public string? DocumentCode { get; set; }
        public long? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public string Direction { get; set; } = string.Empty;
        public int DirectionValue { get; set; }
        public string Type { get; set; } = string.Empty;
        public int TypeValue { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public DateTime BusinessDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Source navigation metadata
        public string? SourceType { get; set; }
        public long? SourceId { get; set; }
        public string? SourceCode { get; set; }
        public long? OrderId { get; set; }
    }
}
