using System;

namespace MenuGoBE.Dtos.Address
{
    public class OldWardViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long OldDistrictId { get; set; }
        public string OldDistrictName { get; set; } = string.Empty;
        public string OldProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
