using System;

namespace MenuGoBE.Dtos.Address
{
    public class OldDistrictViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long OldProvinceId { get; set; }
        public string OldProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
