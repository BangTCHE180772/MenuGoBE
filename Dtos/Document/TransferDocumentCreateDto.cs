using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class TransferDocumentCreateDto
    {
        [Required]
        public long FromBranchId { get; set; }

        [Required]
        public long ToBranchId { get; set; }

        [Required]
        public DateTime BusinessDate { get; set; }

        public string? Note { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm chuyển kho.")]
        public List<TransferDocumentDetailDto> Details { get; set; } = new List<TransferDocumentDetailDto>();

        public List<TransferDocumentPartnerDto> Partners { get; set; } = new List<TransferDocumentPartnerDto>();
    }

    public class TransferDocumentDetailDto
    {
        [Required]
        public long FromBInventoryId { get; set; }

        [Required]
        public long ToBInventoryId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng chuyển phải lớn hơn 0.")]
        public decimal Quantity { get; set; }

        public string? Note { get; set; }
    }

    public class TransferDocumentPartnerDto
    {
        [Required]
        public long PartnerId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Chi phí vận chuyển không được âm.")]
        public decimal Cost { get; set; }

        public string? Note { get; set; }
    }
}
