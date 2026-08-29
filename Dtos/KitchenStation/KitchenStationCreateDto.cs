namespace MenuGoBE.Dtos.KitchenStation
{
    public class KitchenStationCreateDto
    {
        public long BranchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
