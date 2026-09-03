namespace MenuGoBE.Models;

public class PointSystemConfig
{
    public int EarnRate_SpendAmount { get; set; } = 100000;
    public int EarnRate_PointReward { get; set; } = 1000;
    public int MinPointsToUse { get; set; } = 50000;
    public int MaxDiscountPercentage { get; set; } = 50;
}
