using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.WorkSchedule
{
    public class WorkScheduleAutoAssignDto
    {
        [Required]
        public long BranchId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        public List<long>? ShiftIds { get; set; }

        public bool OverwriteExisting { get; set; } = false;

        public long CreatedBy { get; set; }
    }

    public class WorkScheduleAutoAssignResultDto
    {
        public int TotalCreated { get; set; }
        public int FullTimeSchedulesCreated { get; set; }
        public int PartTimeSchedulesCreated { get; set; }
        public int SkippedSlotsCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
