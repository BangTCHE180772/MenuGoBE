namespace MenuGoBE.Dtos.Address
{
    public class AddressViewDto
    {
        public long Id { get; set; }
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public string? AddressDetail { get; set; }
    }
}
