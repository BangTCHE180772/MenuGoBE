using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Customer
{
    /// <summary>
    /// DTO hiển thị thông tin khách hàng (Tuyệt đối không chứa Password/PasswordHash)
    /// </summary>
    public class CustomerViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Point { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool ReceivePromoEmails { get; set; }
    }

    /// <summary>
    /// DTO cập nhật profile khách hàng (Chỉ cho phép cập nhật Name, Phone, Email - Không chứa password)
    /// </summary>
    public class CustomerProfileUpdateDto
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO tạo khách hàng mới từ trang quản lý
    /// </summary>
    public class CustomerCreateDto
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public int InitialPoint { get; set; } = 0;
    }

    /// <summary>
    /// DTO yêu cầu điều chỉnh điểm thủ công
    /// </summary>
    public class PointAdjustmentDto
    {
        [Range(0, 10000000, ErrorMessage = "Số điểm mới phải từ 0 trở lên")]
        public int NewPoints { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do điều chỉnh điểm")]
        [MaxLength(255, ErrorMessage = "Lý do không được vượt quá 255 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO sửa đổi bản ghi điều chỉnh điểm thủ công
    /// </summary>
    public class PointTransactionEditDto
    {
        [Range(0, 10000000, ErrorMessage = "Số điểm sau điều chỉnh phải từ 0 trở lên")]
        public int NewPoints { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do điều chỉnh")]
        [MaxLength(255, ErrorMessage = "Lý do không được vượt quá 255 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO hiển thị bản ghi lịch sử điểm của khách hàng
    /// </summary>
    public class CustomerPointTransactionDto
    {
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; } = string.Empty;
        public PointTransactionType TypeEnum { get; set; }
        public string? RelatedCode { get; set; }
        public long? OrderId { get; set; }
        public int PointChange { get; set; }
        public int PointBefore { get; set; }
        public int PointAfter { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsManual { get; set; }
        public bool IsReversed { get; set; }
    }
}
