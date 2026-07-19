using System;

namespace MenuGoBE.Dtos.Shift
{
    public class ShiftUpdateDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}
