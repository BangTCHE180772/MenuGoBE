using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Document
{
    public class TransferReceiveDocumentCreateDto
    {
        [Required]
        public long TransferDocumentId { get; set; }

        [Required]
        public DateTime ReceivedDate { get; set; }

        /// <summary>
        /// Bắt buộc: Id tài khoản người nhận (truyền từ Frontend).
        /// </summary>
        [Required]
        public long CreatedByAccountId { get; set; }

        public string? Note { get; set; }


        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm nhận kho.")]
        public List<TransferReceiveDocumentDetailDto> Details { get; set; } = new List<TransferReceiveDocumentDetailDto>();
    }

    public class TransferReceiveDocumentDetailDto
    {
        [Required]
        public long ToBInventoryId { get; set; }

        [Required]
        [Range(0.001, 1000000.000, ErrorMessage = "Số lượng nhận phải từ 0,001 đến 1.000.000 đơn vị.")]
        public decimal ReceivedQuantity { get; set; }

        public string? Note { get; set; }
    }

}
