using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Document
{
    /// <summary>
    /// DTO yêu cầu tạo mới Phiếu Khách trả hàng (Customer Return Document).
    /// </summary>
    public class CustomerReturnDocumentCreateDto
    {
        [Required(ErrorMessage = "Mã chi nhánh không được để trống.")]
        public long BranchId { get; set; }

        public long? PartnerId { get; set; }

        public long? ParentDocumentId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Note { get; set; }

        #region Thông tin Thanh toán / Hoàn tiền
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public decimal AmountPaid { get; set; } = 0;
        #endregion

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng khách trả không được để trống.")]
        public List<CustomerReturnDocumentDetailCreateDto> Details { get; set; } = new List<CustomerReturnDocumentDetailCreateDto>();
    }

    /// <summary>
    /// DTO chi tiết từng dòng sản phẩm khi tạo mới Phiếu Khách trả hàng.
    /// </summary>
    public class CustomerReturnDocumentDetailCreateDto
    {
        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }
        public long? FatherId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng trả phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá trả không được nhỏ hơn 0.")]
        public decimal UnitPrice { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu cập nhật Phiếu Khách trả hàng PENDING.
    /// </summary>
    public class CustomerReturnDocumentUpdateDto
    {
        public long? PartnerId { get; set; }
        public long? ParentDocumentId { get; set; }
        public DateTime OrderDate { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public decimal AmountPaid { get; set; } = 0;

        [Required(ErrorMessage = "Danh sách chi tiết mặt hàng khách trả không được để trống.")]
        public List<CustomerReturnDocumentDetailUpdateDto> Details { get; set; } = new List<CustomerReturnDocumentDetailUpdateDto>();
    }

    /// <summary>
    /// DTO chi tiết từng dòng sản phẩm khi cập nhật Phiếu Khách trả hàng PENDING.
    /// </summary>
    public class CustomerReturnDocumentDetailUpdateDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "BInventoryId không được để trống.")]
        public long BInventoryId { get; set; }

        public long? UnitConversionId { get; set; }
        public long? FatherId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng trả phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá trả không được nhỏ hơn 0.")]
        public decimal UnitPrice { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
