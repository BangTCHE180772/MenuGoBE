using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Contract
{
    public class ContractUpdateDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long AccountId { get; set; }

        [Required]
        public long RoleId { get; set; }

        [Required]
        public long BranchId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public string SalaryType { get; set; } = string.Empty;

        [Required]
        public decimal BaseSalary { get; set; }

        public int? BaseWorkDay { get; set; }
    }
}
