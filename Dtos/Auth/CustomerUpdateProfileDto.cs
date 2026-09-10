namespace MenuGoBE.Dtos.Auth
{
    public class CustomerUpdateProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public bool ReceivePromoEmails { get; set; } = true;
    }
}
