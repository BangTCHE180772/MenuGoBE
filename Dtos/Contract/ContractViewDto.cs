using System;

namespace MenuGoBE.Dtos.Contract
{
    public class ContractViewDto
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public long RoleId { get; set; }
        public long BranchId { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string SalaryType { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public int? BaseWorkDay { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
