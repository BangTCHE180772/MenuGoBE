using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerViewDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }
        public PartnerType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? AddressId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal RemainingDebt { get; set; } = 0;
        public System.Collections.Generic.List<string> ImageUrls { get; set; } = new System.Collections.Generic.List<string>();
    }
}
