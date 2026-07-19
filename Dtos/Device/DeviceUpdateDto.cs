namespace MenuGoBE.Dtos.Device
{
    public class DeviceUpdateDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
