using System;
using System.Collections.Generic;

namespace MenuGoBE.Dtos.Account
{
    public class AccountViewDto
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
        public DateTime CreatedAt { get; set; }
        public List<long> RoleIds { get; set; } = new List<long>();
        public long? BranchId { get; set; }
    }
}
