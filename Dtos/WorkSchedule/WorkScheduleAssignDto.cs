using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class WorkScheduleAssignDto
    {
        [Required]
        public List<long> AccountIds { get; set; } = new();

        [Required]
        public long BranchId { get; set; }

        [Required]
        public long ShiftId { get; set; }

        [Required]
        public List<DateOnly> WorkDates { get; set; } = new();

        [Required]
        public long CreatedBy { get; set; }
    }
}
