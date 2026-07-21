using System;

namespace MenuGoBE.Dtos.Address
{
    public class NewProvinceViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
