using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class PayrollService : IPayrollService
    {
        private readonly IPayrollRepository _payrollRepo;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public PayrollService(IPayrollRepository payrollRepo, AppDbContext context, IMapper mapper)
        {
            _payrollRepo = payrollRepo;
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<PayrollViewDto>> GetAllAsync(long? branchId = null, int? month = null, int? year = null, long? accountId = null, List<long>? roleIds = null)
        {
            var payrolls = await _payrollRepo.GetAllAsync(branchId, month, year, accountId, roleIds);
            return _mapper.Map<List<PayrollViewDto>>(payrolls);
        }

        public async Task<PayrollViewDto?> GetByIdAsync(long id)
        {
            var payroll = await _payrollRepo.GetByIdAsync(id);
            return payroll == null ? null : _mapper.Map<PayrollViewDto>(payroll);
        }

        public async Task<PayrollViewDto> CreateAsync(PayrollCreateDto dto)
        {
            var existing = await _payrollRepo.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year);
            if (existing != null)
            {
                throw new InvalidOperationException($"Nhân viên ID {dto.AccountId} đã có bảng lương cho tháng {dto.Month}/{dto.Year}");
            }

            var entity = _mapper.Map<Payroll>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            var contract = account?.Contracts.FirstOrDefault(c => c.Status == "Active")
                           ?? account?.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

            if (contract != null && contract.BranchId > 0)
            {
                entity.BranchId = contract.BranchId;
                entity.ContractId = contract.Id;
            }

            // Recalculate totals from details if present
            RecalculateTotals(entity);

            await _payrollRepo.CreateAsync(entity);

            var created = await _payrollRepo.GetByIdAsync(entity.Id);
            return _mapper.Map<PayrollViewDto>(created!);
        }

        public async Task<bool> UpdateAsync(PayrollUpdateDto dto)
        {
            var entity = await _payrollRepo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            if (entity.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Không thể cập nhật bảng lương đã thanh toán (Paid)");
            }

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == entity.AccountId);

            var contract = account?.Contracts.FirstOrDefault(c => c.Status == "Active")
                           ?? account?.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

            if (contract != null && contract.BranchId > 0)
            {
                entity.BranchId = contract.BranchId;
                if (entity.ContractId == null || entity.ContractId <= 0)
                {
                    entity.ContractId = contract.Id;
                }
            }

            entity.Month = dto.Month;
            entity.Year = dto.Year;
            entity.SalaryType = dto.SalaryType;
            entity.BaseSalary = dto.BaseSalary;
            entity.BaseWorkDays = dto.BaseWorkDays;
            entity.ActualWorkDays = dto.ActualWorkDays;
            entity.ActualWorkHours = dto.ActualWorkHours;
            entity.CalculatedSalary = dto.CalculatedSalary;
            entity.TotalAllowance = dto.TotalAllowance;
            entity.BonusAmount = dto.BonusAmount;
            entity.TotalDeduction = dto.TotalDeduction;
            entity.PenaltyAmount = dto.PenaltyAmount;
            entity.NetSalary = dto.NetSalary;
            entity.Status = dto.Status;
            entity.PaymentDate = dto.PaymentDate;
            entity.Note = dto.Note;

            // Update salary details
            if (dto.SalaryDetails != null)
            {
                entity.SalaryDetails.Clear();
                foreach (var item in dto.SalaryDetails)
                {
                    entity.SalaryDetails.Add(new SalaryDetail
                    {
                        PayrollId = entity.Id,
                        AccountId = item.AccountId > 0 ? item.AccountId : entity.AccountId,
                        Title = item.Title,
                        Type = item.Type,
                        Amount = item.Amount,
                        Note = item.Note,
                        CreatedBy = item.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            RecalculateTotals(entity);

            await _payrollRepo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _payrollRepo.GetByIdAsync(id);
            if (entity == null) return false;

            if (entity.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Không thể xóa bảng lương đã thanh toán");
            }

            await _payrollRepo.DeleteAsync(id);
            return true;
        }

        public async Task<bool> UpdateStatusAsync(long id, PayrollStatus status, DateTime? paymentDate = null)
        {
            var entity = await _payrollRepo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Status = status;
            if (status == PayrollStatus.Paid)
            {
                var dt = paymentDate ?? DateTime.UtcNow;
                entity.PaymentDate = dt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : dt.ToUniversalTime();
            }
            else
            {
                entity.PaymentDate = null;
            }

            await _payrollRepo.UpdateAsync(entity);
            return true;
        }

        public async Task<PayrollViewDto> GenerateEmployeePayrollAsync(PayrollGenerateDto dto)
        {
            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy nhân viên với ID {dto.AccountId}");
            }

            var contract = account.Contracts.FirstOrDefault(c => c.Status == "Active")
                           ?? account.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

            // Fetch work schedules for account in Month/Year
            var startDate = new DateOnly(dto.Year, dto.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var schedules = await _context.WorkSchedules
                .Where(w => w.AccountId == dto.AccountId 
                         && w.WorkDate >= startDate 
                         && w.WorkDate <= endDate
                         && (w.Status == "APPROVED" || w.Status == "COMPLETED" || w.ManagerApproved))
                .ToListAsync();

            decimal actualWorkDays = schedules.Select(s => s.WorkDate).Distinct().Count();
            decimal actualWorkHours = schedules.Sum(s => s.ActualHours ?? 0);

            decimal baseSalary = contract?.BaseSalary ?? 0;
            int baseWorkDays = contract?.BaseWorkDay > 0 ? contract.BaseWorkDay : 26;
            string salaryType = contract?.SalaryType ?? "Monthly";

            decimal calculatedSalary = 0;
            if (salaryType.Equals("Hourly", StringComparison.OrdinalIgnoreCase))
            {
                calculatedSalary = baseSalary * actualWorkHours;
            }
            else
            {
                calculatedSalary = baseWorkDays > 0 ? (baseSalary * actualWorkDays / baseWorkDays) : baseSalary;
            }

            var existingPayroll = await _payrollRepo.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year);
            if (existingPayroll != null)
            {
                if (contract != null && contract.BranchId > 0)
                {
                    existingPayroll.BranchId = contract.BranchId;
                }
                existingPayroll.BaseSalary = baseSalary;
                existingPayroll.BaseWorkDays = baseWorkDays;
                existingPayroll.ActualWorkDays = actualWorkDays;
                existingPayroll.ActualWorkHours = actualWorkHours;
                existingPayroll.CalculatedSalary = Math.Round(calculatedSalary, 2);
                RecalculateTotals(existingPayroll);

                await _payrollRepo.UpdateAsync(existingPayroll);
                var updated = await _payrollRepo.GetByIdAsync(existingPayroll.Id);
                return _mapper.Map<PayrollViewDto>(updated!);
            }
            else
            {
                var payroll = new Payroll
                {
                    AccountId = dto.AccountId,
                    BranchId = (contract != null && contract.BranchId > 0) ? contract.BranchId : (dto.BranchId > 0 ? dto.BranchId : 1),
                    ContractId = contract?.Id,
                    Month = dto.Month,
                    Year = dto.Year,
                    SalaryType = salaryType,
                    BaseSalary = baseSalary,
                    BaseWorkDays = baseWorkDays,
                    ActualWorkDays = actualWorkDays,
                    ActualWorkHours = actualWorkHours,
                    CalculatedSalary = Math.Round(calculatedSalary, 2),
                    TotalAllowance = 0,
                    BonusAmount = 0,
                    TotalDeduction = 0,
                    PenaltyAmount = 0,
                    NetSalary = Math.Round(calculatedSalary, 2),
                    Status = PayrollStatus.Draft,
                    CreatedBy = dto.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _payrollRepo.CreateAsync(payroll);
                var created = await _payrollRepo.GetByIdAsync(payroll.Id);
                return _mapper.Map<PayrollViewDto>(created!);
            }
        }

        public async Task<List<PayrollViewDto>> GenerateBatchPayrollAsync(PayrollBatchGenerateDto dto, bool isAdmin, bool isManager, List<long> managerBranchIds)
        {
            var results = new List<PayrollViewDto>();

            var accountsQuery = _context.Accounts
                .Include(a => a.Contracts)
                .AsQueryable();

            if (isAdmin)
            {
                accountsQuery = accountsQuery.Where(a => a.Contracts.Any(c => c.RoleId == 3));
                if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
                {
                    accountsQuery = accountsQuery.Where(a => a.Contracts.Any(c => c.BranchId == dto.BranchId.Value));
                }
            }
            else if (isManager)
            {
                accountsQuery = accountsQuery.Where(a => a.Contracts.Any(c => (c.RoleId == 4 || c.RoleId == 5 || c.RoleId == 6)
                                                                            && managerBranchIds.Contains(c.BranchId)));
                if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
                {
                    if (managerBranchIds.Contains(dto.BranchId.Value))
                    {
                        accountsQuery = accountsQuery.Where(a => a.Contracts.Any(c => c.BranchId == dto.BranchId.Value));
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Bạn không có quyền tính bảng lương cho chi nhánh khác.");
                    }
                }
            }
            else
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tính toán bảng lương.");
            }

            var eligibleAccounts = await accountsQuery.ToListAsync();

            foreach (var acc in eligibleAccounts)
            {
                var contract = acc.Contracts.FirstOrDefault(c => c.Status == "Active")
                               ?? acc.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

                if (contract == null) continue;

                var singleDto = new PayrollGenerateDto
                {
                    AccountId = acc.Id,
                    BranchId = contract.BranchId,
                    Month = dto.Month,
                    Year = dto.Year,
                    CreatedBy = dto.CreatedBy
                };

                try
                {
                    var generated = await GenerateEmployeePayrollAsync(singleDto);
                    results.Add(generated);
                }
                catch
                {
                    // Continue batch calculation
                }
            }

            return results;
        }

        private static void RecalculateTotals(Payroll payroll)
        {
            if (payroll.SalaryDetails != null && payroll.SalaryDetails.Any())
            {
                payroll.TotalAllowance = payroll.SalaryDetails
                    .Where(d => d.Type == SalaryAdjustmentType.Allowance)
                    .Sum(d => d.Amount);

                payroll.BonusAmount = payroll.SalaryDetails
                    .Where(d => d.Type == SalaryAdjustmentType.Bonus)
                    .Sum(d => d.Amount);

                payroll.TotalDeduction = payroll.SalaryDetails
                    .Where(d => d.Type == SalaryAdjustmentType.Deduction)
                    .Sum(d => d.Amount);

                payroll.PenaltyAmount = payroll.SalaryDetails
                    .Where(d => d.Type == SalaryAdjustmentType.Penalty)
                    .Sum(d => d.Amount);
            }

            payroll.NetSalary = payroll.CalculatedSalary 
                              + payroll.TotalAllowance 
                              + payroll.BonusAmount 
                              - payroll.TotalDeduction 
                              - payroll.PenaltyAmount;
        }
    }
}
