using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Account
{
    public class AccountUpdateDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string AvatarImage { get; set; } = string.Empty;

        public string CitizenIdCode { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public long? AddressId { get; set; }
    }
}
