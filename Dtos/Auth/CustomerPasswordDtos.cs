namespace MenuGoBE.Dtos.Auth
{
    public class CustomerSendOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class CustomerResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
