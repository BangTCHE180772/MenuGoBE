using System;

namespace MenuGoBE.Dtos.Shift
{
    public class ShiftCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; }
        public long CreatedBy { get; set; }
    }
}
