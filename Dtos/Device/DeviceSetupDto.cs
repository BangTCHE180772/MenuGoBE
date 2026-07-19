namespace MenuGoBE.Dtos.Device
{
    public class DeviceSetupDto
    {
        public string Name { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public long? BranchId { get; set; }
    }
}
