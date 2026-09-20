using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerUpdateDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public long? AddressId { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        public System.Collections.Generic.List<string>? ImageUrls { get; set; }
    }
}
