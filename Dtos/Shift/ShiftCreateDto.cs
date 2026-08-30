using System;
using System.Collections.Generic;

namespace MenuGoBE.Dtos.Shift
{
    public class ShiftCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsNightShift { get; set; } = false;
        public decimal NightBonusRate { get; set; } = 1.3m;
        public decimal NightAllowance { get; set; } = 0m;
        public TimeOnly? NightStartTime { get; set; }
        public TimeOnly? NightEndTime { get; set; }
        public decimal NightHours { get; set; } = 0m;
        public decimal StandardHours { get; set; } = 8.0m;
        public bool IsActive { get; set; } = true;
        public string ApplicableTo { get; set; } = "ALL";
        public long CreatedBy { get; set; }
        public List<ShiftRoleRequirementDto> RoleRequirements { get; set; } = new();
    }
}

