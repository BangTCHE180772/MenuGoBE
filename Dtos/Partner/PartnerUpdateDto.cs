using System.ComponentModel.DataAnnotations;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerUpdateDto
    {
        [Required(ErrorMessage = "Tên đối tác là bắt buộc.")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public long? AddressId { get; set; }

        public PartnerType? Type { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public System.Collections.Generic.List<string>? ImageUrls { get; set; }
    }
}
