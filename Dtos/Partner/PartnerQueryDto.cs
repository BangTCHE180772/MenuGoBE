using MenuGoBE.Dtos.Base;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Partner
{
    public class PartnerQueryDto : BaseQueryDto
    {
        public long? BranchId { get; set; }
        
        public string? Search { get; set; } // Search by Name, Phone, Email
        
        public PartnerType? Type { get; set; }

        /// <summary>
        /// Mode 1: Supplier (PartnerType.Supplier)
        /// Mode 2: Transport and Other (PartnerType.Transporter, PartnerType.Other)
        /// Mode 3: Customer (PartnerType.Customer)
        /// </summary>
        public int? Mode { get; set; }
    }
}
