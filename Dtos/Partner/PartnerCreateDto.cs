using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerCreateDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public PartnerType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public long? AddressId { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [MaxLength(255)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        public System.Collections.Generic.List<string>? ImageUrls { get; set; }
    }
}
