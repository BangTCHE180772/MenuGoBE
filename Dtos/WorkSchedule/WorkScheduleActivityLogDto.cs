namespace MenuGoBE.Dtos.WorkSchedule;

public class WorkScheduleActivityLogDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}
