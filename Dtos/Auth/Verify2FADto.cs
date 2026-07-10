namespace MenuGoBE.Dtos.Auth;

public class Verify2FADto
{
    public long AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
}
