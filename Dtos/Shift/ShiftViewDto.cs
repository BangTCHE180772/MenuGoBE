using System;
using System.Collections.Generic;

namespace MenuGoBE.Dtos.Shift
{
    public class ShiftViewDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsNightShift { get; set; }
        public decimal NightBonusRate { get; set; }
        public decimal NightAllowance { get; set; }
        public TimeOnly? NightStartTime { get; set; }
        public TimeOnly? NightEndTime { get; set; }
        public decimal NightHours { get; set; }
        public decimal StandardHours { get; set; }
        public bool IsActive { get; set; }
        public string ApplicableTo { get; set; } = "ALL";
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ShiftRoleRequirementDto> RoleRequirements { get; set; } = new();
    }
}

