namespace MenuGoBE.Dtos.Menu
{
    public class MenuProductViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }
        public bool IsSellable { get; set; }
        public bool IsActive { get; set; }

        public long? GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }
}
