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
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Logging.ILogger<PayrollService> _logger;

        public PayrollService(IPayrollRepository payrollRepo, AppDbContext context, IMapper mapper, IEmailService emailService, Microsoft.Extensions.Logging.ILogger<PayrollService> logger)
        {
            _payrollRepo = payrollRepo;
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
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

        public async Task<List<PayrollViewDto>> GetByAccountIdAsync(long accountId, int? month = null, int? year = null)
        {
            var payrolls = await _payrollRepo.GetByAccountIdAsync(accountId, month, year);
            return _mapper.Map<List<PayrollViewDto>>(payrolls);
        }

        public async Task<PayrollViewDto> CreateAsync(PayrollCreateDto dto)
        {
            var now = DateTime.UtcNow;
            if (dto.Year > now.Year || (dto.Year == now.Year && dto.Month > now.Month))
            {
                throw new InvalidOperationException($"Không thể tạo bảng lương cho các tháng trong tương lai (tháng {dto.Month}/{dto.Year}).");
            }

            var existing = await _payrollRepo.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year);
            if (existing != null)
            {
                throw new InvalidOperationException($"Nhân viên ID {dto.AccountId} đã có bảng lương cho tháng {dto.Month}/{dto.Year}");
            }

            var entity = _mapper.Map<Payroll>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            var monthStart = new DateOnly(dto.Year, dto.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            var contract = account?.Contracts
                .Where(c => c.StartDate <= monthEnd && (!c.EndDate.HasValue || c.EndDate.Value >= monthStart))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (contract == null)
            {
                throw new InvalidOperationException($"Nhân viên {(account != null ? account.Name : "ID " + dto.AccountId)} không có hợp đồng hợp lệ trong tháng {dto.Month}/{dto.Year}.");
            }

            if (contract.BranchId > 0)
            {
                entity.BranchId = contract.BranchId;
                entity.ContractId = contract.Id;
            }

            // Recalculate totals from details if present
            RecalculateTotals(entity);

            await _payrollRepo.CreateAsync(entity);
            await SendPayrollStatusEmailAsync(entity, entity.Status);

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

            var monthStart = new DateOnly(dto.Year, dto.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == entity.AccountId);

            var contract = account?.Contracts
                .Where(c => c.StartDate <= monthEnd && (!c.EndDate.HasValue || c.EndDate.Value >= monthStart))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

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
            var oldStatus = entity.Status;
            entity.Status = dto.Status;
            entity.PaymentDate = dto.PaymentDate;
            entity.Note = dto.Note;

            // Update salary details
            if (dto.SalaryDetails != null)
            {
                if (entity.SalaryDetails != null && entity.SalaryDetails.Any())
                {
                    _context.SalaryDetails.RemoveRange(entity.SalaryDetails);
                    entity.SalaryDetails.Clear();
                }
                else
                {
                    entity.SalaryDetails = new List<SalaryDetail>();
                }

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
                        CreatedBy = item.CreatedBy > 0 ? item.CreatedBy : entity.AccountId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            RecalculateTotals(entity);

            await _payrollRepo.UpdateAsync(entity);

            if (oldStatus != entity.Status)
            {
                await SendPayrollStatusEmailAsync(entity, entity.Status);
            }

            if (oldStatus != PayrollStatus.Paid && entity.Status == PayrollStatus.Paid)
            {
                await CreateCashFlowForPayrollAsync(entity);
            }

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

            if (entity.Status == PayrollStatus.Draft && status != PayrollStatus.Draft && status != PayrollStatus.Pending && status != PayrollStatus.Cancelled)
            {
                throw new InvalidOperationException("Bảng lương ở trạng thái Bản nháp chỉ được phép đổi sang Chờ thanh toán/Chờ chốt hoặc Hủy.");
            }

            if (status == PayrollStatus.Paid && entity.Status != PayrollStatus.Approved)
            {
                throw new InvalidOperationException("Bảng lương phải được Owner phê duyệt (Approved) trước khi thanh toán.");
            }

            var oldStatus = entity.Status;
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

            if (oldStatus != status)
            {
                await SendPayrollStatusEmailAsync(entity, status);
            }

            if (oldStatus != PayrollStatus.Paid && status == PayrollStatus.Paid)
            {
                await CreateCashFlowForPayrollAsync(entity);
            }

            return true;
        }

        #region Tự động tạo phiếu chi lương nhân viên (PLNV)
        private async Task CreateCashFlowForPayrollAsync(Payroll payroll)
        {
            if (payroll.NetSalary <= 0) return;

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == payroll.AccountId);
            var employeeName = account?.Name ?? $"ID {payroll.AccountId}";

            // Khởi tạo mã phiếu tạm thời với tiền tố PLNV
            var tempCode = $"PLNV{DateTime.UtcNow.Ticks % 1_000_000:D6}";
            var cashFlow = new CashFlow
            {
                BranchId = payroll.BranchId,
                Code = tempCode,
                BusinessDate = payroll.PaymentDate ?? DateTime.UtcNow,
                Direction = CashFlowDirection.Outflow,
                Type = CashFlowDetailType.Payment,
                PaymentMethod = PaymentMethod.Cash,
                Status = CashFlowStatus.Completed,
                TotalAmount = payroll.NetSalary,
                Note = $"Chi trả lương tháng {payroll.Month}/{payroll.Year} cho nhân viên {employeeName}",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.CashFlows.AddAsync(cashFlow);
            await _context.SaveChangesAsync();

            // Cập nhật mã phiếu chính thức sau khi có Id tự tăng
            cashFlow.Code = $"PLNV{cashFlow.Id:D6}";
            _context.CashFlows.Update(cashFlow);
            await _context.SaveChangesAsync();
        }
        #endregion

        public async Task<bool> LockPayrollAsync(long id, long userId)
        {
            var entity = await _payrollRepo.GetByIdAsync(id);
            if (entity == null) return false;

            if (entity.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Bảng lương đã thanh toán không thể khóa lại.");
            }

            entity.Status = PayrollStatus.Locked;
            entity.LockedBy = userId;
            entity.LockedAt = DateTime.UtcNow;

            await _payrollRepo.UpdateAsync(entity);
            await SendPayrollStatusEmailAsync(entity, PayrollStatus.Locked);
            return true;
        }

        public async Task<bool> UnlockPayrollAsync(long id, long userId)
        {
            var entity = await _payrollRepo.GetByIdAsync(id);
            if (entity == null) return false;

            if (entity.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Bảng lương đã thanh toán không thể mở khóa.");
            }

            if (entity.Status != PayrollStatus.Locked && entity.Status != PayrollStatus.Approved)
            {
                throw new InvalidOperationException("Bảng lương phải ở trạng thái Đã chốt hoặc Đã duyệt mới có thể mở khóa.");
            }

            entity.Status = PayrollStatus.Draft;
            entity.LockedBy = null;
            entity.LockedAt = null;
            entity.ApprovedBy = null;
            entity.ApprovedAt = null;

            await _payrollRepo.UpdateAsync(entity);
            await SendPayrollStatusEmailAsync(entity, PayrollStatus.Draft);
            return true;
        }

        public async Task<bool> ApprovePayrollAsync(long id, long userId)
        {
            var entity = await _payrollRepo.GetByIdAsync(id);
            if (entity == null) return false;

            if (entity.Status == PayrollStatus.Paid)
            {
                throw new InvalidOperationException("Bảng lương đã thanh toán không thể phê duyệt lại.");
            }

            entity.Status = PayrollStatus.Approved;
            entity.ApprovedBy = userId;
            entity.ApprovedAt = DateTime.UtcNow;

            await _payrollRepo.UpdateAsync(entity);
            await SendPayrollStatusEmailAsync(entity, PayrollStatus.Approved);
            return true;
        }

        public async Task<PayrollViewDto> GenerateEmployeePayrollAsync(PayrollGenerateDto dto)
        {
            var now = DateTime.UtcNow;
            if (dto.Year > now.Year || (dto.Year == now.Year && dto.Month > now.Month))
            {
                throw new InvalidOperationException($"Không thể tự động tính bảng lương cho các tháng trong tương lai (tháng {dto.Month}/{dto.Year}).");
            }

            var startDate = new DateOnly(dto.Year, dto.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

            if (account == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy nhân viên với ID {dto.AccountId}");
            }

            var contract = account.Contracts
                .Where(c => c.StartDate <= endDate && (!c.EndDate.HasValue || c.EndDate.Value >= startDate))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (contract == null)
            {
                throw new InvalidOperationException($"Nhân viên {account.Name} không có hợp đồng hợp lệ trong tháng {dto.Month}/{dto.Year}.");
            }

            long branchId = contract.BranchId > 0 ? contract.BranchId : (dto.BranchId > 0 ? dto.BranchId : 1);

            // Fetch active holiday configurations for date range
            var holidayConfigs = await _context.HolidayConfigs
                .Where(h => h.IsActive 
                         && (h.BranchId == null || h.BranchId == branchId)
                         && h.FromDate <= endDate 
                         && h.ToDate >= startDate)
                .ToListAsync();

            // Fetch approved work schedules for the month
            var schedules = await _context.WorkSchedules
                .Include(w => w.Shift)
                .Where(w => w.AccountId == dto.AccountId 
                         && w.WorkDate >= startDate 
                         && w.WorkDate <= endDate
                         && (
                             w.Status == "Completed"
                             || w.Status == "PRESENT"
                             || w.Status == "PRESENT_APPROVED"
                             || w.Status == "CÓ MẶT"
                             || w.Status == "ĐÃ HOÀN THÀNH"
                             || w.ManagerApproved
                             || (w.CheckInAt != null && w.CheckOutAt != null)
                             || (w.ActualHours.HasValue && w.ActualHours.Value > 0)
                         ))
                .ToListAsync();

            decimal actualWorkDays = schedules.Select(s => s.WorkDate).Distinct().Count();
            decimal actualWorkHours = schedules.Sum(s => (s.ActualHours ?? 0) + s.ApprovedOTHours);

            decimal baseSalary = contract.BaseSalary;
            string salaryType = contract.SalaryType ?? "Monthly";
            bool isHourly = salaryType.Equals("Hourly", StringComparison.OrdinalIgnoreCase);

            int baseWorkDays = (isHourly || contract.RoleId == 3) ? 0 : (contract.BaseWorkDay ?? 26);
            if (baseWorkDays <= 0 && !isHourly && contract.RoleId != 3) baseWorkDays = 26;

            // Calculate base hourly rate
            decimal hourlyRate = 0m;
            if (isHourly)
            {
                hourlyRate = baseSalary;
            }
            else
            {
                hourlyRate = baseWorkDays > 0 ? (baseSalary / (baseWorkDays * 8.0m)) : (baseSalary / 208.0m);
            }

            decimal totalBaseShiftPay = 0m;
            decimal totalNightPay = 0m;
            decimal totalOTPay = 0m;
            decimal totalHolidayBonus = 0m;

            var shiftDetailsList = new List<PayrollShiftDetail>();

            if (contract.RoleId == 3)
            {
                // Manager role: fixed salary
                totalBaseShiftPay = baseSalary;
            }
            else
            {
                foreach (var s in schedules)
                {
                    var workDate = s.WorkDate;
                    decimal actualHours = s.ActualHours ?? 0m;
                    decimal standardHours = s.Shift != null && s.Shift.StandardHours > 0 ? s.Shift.StandardHours : 8.0m;
                    decimal baseHours = Math.Min(actualHours, standardHours);
                    decimal approvedOTHours = s.ApprovedOTHours;

                    // Evaluate coefficients: Tet / Holiday vs. Normal
                    var matchingHolidays = holidayConfigs
                        .Where(h => h.IsActive && IsDateAndBranchMatchingHoliday(workDate, s.BranchId, h))
                        .ToList();

                    decimal holidayCoeff = matchingHolidays.Any() ? matchingHolidays.Max(h => h.Coefficient) : 1.0m;

                    // PRIORITIZATION RULE: Pick Max(Coefficients) - Holiday/Tet coefficient (e.g. 3.0) or Normal (1.0)
                    decimal dateCoeff = Math.Max(1.0m, holidayCoeff);

                    // Base shift pay for regular hours
                    decimal baseShiftPay = baseHours * hourlyRate * dateCoeff;

                    // Night shift calculation
                    bool isNightShift = s.Shift?.IsNightShift ?? false;
                    decimal nightShiftPay = 0m;
                    if (isNightShift)
                    {
                        decimal nightBonusRate = s.Shift?.NightBonusRate ?? 1.3m;
                        decimal nightAllowance = s.Shift?.NightAllowance ?? 0m;
                        
                        decimal nightHours = baseHours;
                        if (s.Shift != null && s.Shift.NightStartTime.HasValue && s.Shift.NightEndTime.HasValue)
                        {
                            decimal calculatedNightHours = CalculateNightHours(s.Shift.StartTime, s.Shift.EndTime, s.Shift.NightStartTime.Value, s.Shift.NightEndTime.Value);
                            nightHours = Math.Min(baseHours, calculatedNightHours);
                        }
                        else if (s.Shift != null && s.Shift.NightHours > 0)
                        {
                            nightHours = Math.Min(baseHours, s.Shift.NightHours);
                        }

                        if (nightAllowance > 0)
                        {
                            nightShiftPay = nightAllowance;
                        }
                        else
                        {
                            nightShiftPay = nightHours * hourlyRate * (nightBonusRate - 1.0m);
                        }
                    }

                    // Approved Overtime (OT) Pay
                    decimal otMultiplier = 1.5m;
                    decimal otPay = approvedOTHours * hourlyRate * dateCoeff * otMultiplier;

                    decimal shiftTotal = baseShiftPay + nightShiftPay + otPay;

                    totalBaseShiftPay += baseShiftPay;
                    totalNightPay += nightShiftPay;
                    totalOTPay += otPay;

                    if (dateCoeff > 1.0m)
                    {
                        totalHolidayBonus += baseShiftPay * (1.0m - (1.0m / dateCoeff));
                    }

                    string note = "";
                    if (matchingHolidays.Any())
                    {
                        note += $"Lễ/Tết ({matchingHolidays.First().Name} x{dateCoeff}) ";
                    }
                    if (isNightShift) note += "Ca đêm ";
                    if (approvedOTHours > 0) note += $"OT {approvedOTHours}h ";

                    shiftDetailsList.Add(new PayrollShiftDetail
                    {
                        WorkScheduleId = s.Id,
                        WorkDate = workDate,
                        ActualHours = actualHours + approvedOTHours,
                        StandardHours = standardHours,
                        ApprovedOTHours = approvedOTHours,
                        DateCoefficient = dateCoeff,
                        IsNightShift = isNightShift,
                        HourlyRate = hourlyRate,
                        BaseShiftPay = Math.Round(baseShiftPay, 2),
                        NightShiftPay = Math.Round(nightShiftPay, 2),
                        OTPay = Math.Round(otPay, 2),
                        TotalShiftPay = Math.Round(shiftTotal, 2),
                        Note = note.Trim()
                    });
                }
            }

            decimal calculatedSalary = totalBaseShiftPay + totalNightPay + totalOTPay;

            var existingPayroll = await _payrollRepo.GetByAccountMonthYearAsync(dto.AccountId, dto.Month, dto.Year);
            if (existingPayroll != null)
            {
                if (existingPayroll.Status == PayrollStatus.Paid)
                {
                    throw new InvalidOperationException($"Bảng lương tháng {dto.Month}/{dto.Year} của nhân viên này đã ở trạng thái ĐÃ THANH TOÁN. Không thể tính toán lại.");
                }

                existingPayroll.BranchId = branchId;
                existingPayroll.HourlyRate = hourlyRate;
                existingPayroll.BaseSalary = baseSalary;
                existingPayroll.BaseWorkDays = baseWorkDays;
                existingPayroll.ActualWorkDays = actualWorkDays;
                existingPayroll.ActualWorkHours = actualWorkHours;
                existingPayroll.TotalBaseShiftPay = Math.Round(totalBaseShiftPay, 2);
                existingPayroll.TotalNightPay = Math.Round(totalNightPay, 2);
                existingPayroll.TotalOTPay = Math.Round(totalOTPay, 2);
                existingPayroll.TotalHolidayBonus = Math.Round(totalHolidayBonus, 2);
                existingPayroll.CalculatedSalary = Math.Round(calculatedSalary, 2);

                // Clear old shift details & attach new ones
                if (existingPayroll.PayrollShiftDetails != null && existingPayroll.PayrollShiftDetails.Any())
                {
                    _context.PayrollShiftDetails.RemoveRange(existingPayroll.PayrollShiftDetails);
                }
                existingPayroll.PayrollShiftDetails = shiftDetailsList;

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
                    BranchId = branchId,
                    ContractId = contract.Id,
                    Month = dto.Month,
                    Year = dto.Year,
                    SalaryType = salaryType,
                    HourlyRate = hourlyRate,
                    BaseSalary = baseSalary,
                    BaseWorkDays = baseWorkDays,
                    ActualWorkDays = actualWorkDays,
                    ActualWorkHours = actualWorkHours,
                    TotalBaseShiftPay = Math.Round(totalBaseShiftPay, 2),
                    TotalNightPay = Math.Round(totalNightPay, 2),
                    TotalOTPay = Math.Round(totalOTPay, 2),
                    TotalHolidayBonus = Math.Round(totalHolidayBonus, 2),
                    CalculatedSalary = Math.Round(calculatedSalary, 2),
                    TotalAllowance = 0,
                    BonusAmount = 0,
                    TotalDeduction = 0,
                    PenaltyAmount = 0,
                    NetSalary = Math.Round(calculatedSalary, 2),
                    Status = PayrollStatus.Draft,
                    CreatedBy = dto.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    PayrollShiftDetails = shiftDetailsList
                };

                await _payrollRepo.CreateAsync(payroll);
                var created = await _payrollRepo.GetByIdAsync(payroll.Id);
                return _mapper.Map<PayrollViewDto>(created!);
            }
        }

        public async Task<List<PayrollViewDto>> GenerateBatchPayrollAsync(PayrollBatchGenerateDto dto, bool isAdmin, bool isManager, List<long> managerBranchIds)
        {
            var now = DateTime.UtcNow;
            if (dto.Year > now.Year || (dto.Year == now.Year && dto.Month > now.Month))
            {
                throw new InvalidOperationException($"Không thể tính lương hàng loạt cho các tháng trong tương lai (tháng {dto.Month}/{dto.Year}).");
            }

            var results = new List<PayrollViewDto>();

            var accountsQuery = _context.Accounts
                .Include(a => a.Contracts)
                .AsQueryable();

            if (isAdmin)
            {
                // Owner / Admin can calculate batch payroll for all roles (Managers & Regular Staff)
                accountsQuery = accountsQuery.Where(a => a.Contracts.Any(c => c.RoleId >= 3));
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
            var accountIds = eligibleAccounts.Select(a => a.Id).ToList();
            var existingPayrolls = await _payrollRepo.GetByAccountsMonthYearAsync(accountIds, dto.Month, dto.Year);
            var existingPayrollsDict = existingPayrolls.ToDictionary(p => p.AccountId);

            var monthStart = new DateOnly(dto.Year, dto.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            foreach (var acc in eligibleAccounts)
            {
                var contract = acc.Contracts
                    .Where(c => c.StartDate <= monthEnd && (!c.EndDate.HasValue || c.EndDate.Value >= monthStart))
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefault();

                if (contract == null) continue;

                existingPayrollsDict.TryGetValue(acc.Id, out var existingPayroll);
                if (existingPayroll != null && existingPayroll.Status == PayrollStatus.Paid)
                {
                    continue;
                }

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

        private static bool IsDateAndBranchMatchingHoliday(DateOnly workDate, long scheduleBranchId, HolidayConfig h)
        {
            if (!h.IsActive) return false;

            // Check branch restriction
            if (!string.IsNullOrWhiteSpace(h.BranchIds))
            {
                var bIds = h.BranchIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => long.TryParse(s.Trim(), out var val) ? val : 0)
                    .Where(v => v > 0);
                if (!bIds.Contains(scheduleBranchId)) return false;
            }
            else if (h.BranchId.HasValue && h.BranchId.Value > 0 && h.BranchId.Value != scheduleBranchId)
            {
                return false;
            }

            if (!h.IsRecurring)
            {
                return h.FromDate <= workDate && workDate <= h.ToDate;
            }

            var startMD = (h.FromDate.Month, h.FromDate.Day);
            var endMD = (h.ToDate.Month, h.ToDate.Day);
            var workMD = (workDate.Month, workDate.Day);

            if (startMD.CompareTo(endMD) <= 0)
            {
                return workMD.CompareTo(startMD) >= 0 && workMD.CompareTo(endMD) <= 0;
            }
            else
            {
                return workMD.CompareTo(startMD) >= 0 || workMD.CompareTo(endMD) <= 0;
            }
        }

        private static decimal CalculateNightHours(TimeOnly shiftStart, TimeOnly shiftEnd, TimeOnly nightStart, TimeOnly nightEnd)
        {
            int sStart = shiftStart.Hour * 60 + shiftStart.Minute;
            int sEnd = shiftEnd.Hour * 60 + shiftEnd.Minute;
            if (sEnd <= sStart) sEnd += 1440;

            int nStart = nightStart.Hour * 60 + nightStart.Minute;
            int nEnd = nightEnd.Hour * 60 + nightEnd.Minute;
            if (nEnd <= nStart) nEnd += 1440;

            int totalOverlapMinutes = 0;
            int[] offsets = new int[] { -1440, 0, 1440 };
            foreach (var offset in offsets)
            {
                int ns = nStart + offset;
                int ne = nEnd + offset;

                int overlapStart = Math.Max(sStart, ns);
                int overlapEnd = Math.Min(sEnd, ne);

                if (overlapEnd > overlapStart)
                {
                    totalOverlapMinutes += (overlapEnd - overlapStart);
                }
            }

            return Math.Round(totalOverlapMinutes / 60.0m, 2);
        }

        #region EMAIL NOTIFICATIONS FOR PAYROLL STATUS
        private async Task SendPayrollStatusEmailAsync(Payroll payroll, PayrollStatus newStatus)
        {
            try
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == payroll.AccountId);
                if (account == null)
                {
                    _logger.LogWarning($"[PAYROLL EMAIL] Account ID {payroll.AccountId} not found for Payroll ID {payroll.Id}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(account.Email))
                {
                    _logger.LogWarning($"[PAYROLL EMAIL] Account '{account.Name}' (ID {account.Id}) has no email address configured in database!");
                    return;
                }

                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == payroll.BranchId);
                string branchName = branch?.Name ?? "Chi nhánh MenuGo";

                string subject = "";
                string statusName = "";
                string messageDetail = "";
                string statusBadgeColor = "#2563eb";

                switch (newStatus)
                {
                    case PayrollStatus.Draft:
                    case PayrollStatus.Pending:
                        statusName = "Chờ chốt";
                        subject = $"[MenuGo] Thông báo bảng lương Tháng {payroll.Month}/{payroll.Year} - {statusName}";
                        statusBadgeColor = "#d97706";
                        messageDetail = $"Bảng lương Tháng {payroll.Month}/{payroll.Year} của bạn tại {branchName} đã được khởi tạo và đang trong trạng thái <b>Chờ chốt</b> để Quản lý rà soát.";
                        break;

                    case PayrollStatus.Locked:
                        statusName = "Đã chốt";
                        subject = $"[MenuGo] Thông báo bảng lương Tháng {payroll.Month}/{payroll.Year} - {statusName}";
                        statusBadgeColor = "#7c3aed";
                        messageDetail = $"Quản lý đã hoàn tất <b>Chốt bảng lương</b> Tháng {payroll.Month}/{payroll.Year} của bạn tại {branchName}. Bảng lương đang chờ Chủ cửa hàng (Owner) phê duyệt và thực hiện chuyển khoản thanh toán.";
                        break;

                    case PayrollStatus.Paid:
                        statusName = "Đã thanh toán";
                        subject = $"[MenuGo] Xác nhận thanh toán lương Tháng {payroll.Month}/{payroll.Year}";
                        statusBadgeColor = "#16a34a";
                        messageDetail = $"Bảng lương Tháng {payroll.Month}/{payroll.Year} của bạn tại {branchName} đã được <b>Thanh toán hoàn tất</b>.";
                        break;

                    default:
                        _logger.LogInformation($"[PAYROLL EMAIL] Status '{newStatus}' does not require email dispatch.");
                        return;
                }

                string body = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);"">
                    <div style=""background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); color: #ffffff; padding: 20px 24px;"">
                        <h2 style=""margin: 0; font-size: 20px;"">MenuGo - Thông Báo Bảng Lương</h2>
                        <p style=""margin: 6px 0 0 0; opacity: 0.85; font-size: 14px;"">{branchName}</p>
                    </div>
                    <div style=""padding: 24px; background-color: #ffffff; color: #334155;"">
                        <p style=""font-size: 16px; margin-top: 0;"">Xin chào <b>{account.Name}</b>,</p>
                        <p style=""font-size: 15px; line-height: 1.6;"">{messageDetail}</p>
                        
                        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin: 20px 0;"">
                            <table style=""width: 100%; border-collapse: collapse; font-size: 14px;"">
                                <tr>
                                    <td style=""padding: 6px 0; color: #64748b;"">Kỳ lương:</td>
                                    <td style=""padding: 6px 0; font-weight: bold; text-align: right;"">Tháng {payroll.Month}/{payroll.Year}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; color: #64748b;"">Trạng thái:</td>
                                    <td style=""padding: 6px 0; text-align: right;""><span style=""background-color: {statusBadgeColor}; color: white; padding: 3px 10px; border-radius: 12px; font-size: 12px; font-weight: bold;"">{statusName}</span></td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; color: #64748b;"">Tổng thực nhận:</td>
                                    <td style=""padding: 6px 0; font-weight: bold; color: #16a34a; font-size: 16px; text-align: right;"">{payroll.NetSalary:N0} VNĐ</td>
                                </tr>
                                {(payroll.PaymentDate.HasValue ? $@"
                                <tr>
                                    <td style=""padding: 6px 0; color: #64748b;"">Ngày thanh toán:</td>
                                    <td style=""padding: 6px 0; font-weight: bold; text-align: right;"">{payroll.PaymentDate.Value.ToString("dd/MM/yyyy")}</td>
                                </tr>" : "")}
                            </table>
                        </div>

                        <p style=""font-size: 13px; color: #64748b; margin-bottom: 0;"">Bạn có thể đăng nhập ứng dụng MenuGo để xem chi tiết phiếu lương cá nhân.</p>
                    </div>
                    <div style=""background-color: #f1f5f9; padding: 12px 24px; text-align: center; font-size: 12px; color: #94a3b8;"">
                        Trân trọng,<br/><b>Ban quản lý MenuGo</b>
                    </div>
                </div>";

                _logger.LogInformation($"[PAYROLL EMAIL] Sending email to '{account.Name}' <{account.Email}> for Payroll ID {payroll.Id}...");
                await _emailService.SendEmailAsync(account.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[PAYROLL EMAIL ERROR] Exception occurred while sending email for Payroll ID {payroll.Id}: {ex.Message}");
            }
        }
        #endregion
    }
}
