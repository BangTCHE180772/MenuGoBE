using System;

namespace MenuGoBE.Dtos.Address
{
    public class NewWardViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long NewProvinceId { get; set; }
        public string NewProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
