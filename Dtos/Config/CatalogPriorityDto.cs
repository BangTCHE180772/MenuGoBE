namespace MenuGoBE.Dtos.Config;

public class CatalogPriorityDto
{
    public List<long> GroupOrder { get; set; } = new();
    public List<long> MenuOrder { get; set; } = new();
    public List<string> GroupNames { get; set; } = new();
    public List<string> MenuNames { get; set; } = new();
}
