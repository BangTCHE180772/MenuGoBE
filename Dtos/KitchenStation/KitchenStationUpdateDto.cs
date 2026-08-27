namespace MenuGoBE.Dtos.KitchenStation
{
    public class KitchenStationUpdateDto
    {
        public long Id { get; set; }

        public long BranchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
