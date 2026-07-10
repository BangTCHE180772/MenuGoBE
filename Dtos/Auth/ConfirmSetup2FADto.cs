namespace MenuGoBE.Dtos.Auth;

public class ConfirmSetup2FADto
{
    public long AccountId { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
