using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Auth;

public class CustomerRegisterDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải chứa ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string? Email { get; set; }
}

public class CustomerLoginDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    public string Password { get; set; } = string.Empty;
}

public class CustomerProfileDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Point { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool ReceivePromoEmails { get; set; }
}

public class CustomerLoginResponseDto
{
    public string Status { get; set; } = "success";
    public string Token { get; set; } = string.Empty;
    public CustomerProfileDto Customer { get; set; } = null!;
}
