using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Account
{
    public class AccountCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string AvatarImage { get; set; } = string.Empty;

        public string CitizenIdCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public long? AddressId { get; set; }

        public long CreatedBy { get; set; }
    }
}
