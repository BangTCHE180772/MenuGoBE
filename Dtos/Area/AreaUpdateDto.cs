namespace MenuGoBE.Dtos.Area
{
    public class AreaUpdateDto
    {
        public long Id { get; set; }

        public long BranchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
