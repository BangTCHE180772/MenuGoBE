using MenuGoBE.Dtos.Base;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerQueryDto : BaseQueryDto
    {
        public long? BranchId { get; set; }
        
        public string? Search { get; set; } // Search by Name, Phone, Email
        
        public PartnerType? Type { get; set; }
    }
}
