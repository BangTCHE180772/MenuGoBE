using MenuGoBE.Data;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Exceptions;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service;

public class PayrollSuggestionService : IPayrollSuggestionService
{
    private readonly IPayrollSuggestionRepository _suggestionRepo;
    private readonly AppDbContext _context;

    public PayrollSuggestionService(IPayrollSuggestionRepository suggestionRepo, AppDbContext context)
    {
        _suggestionRepo = suggestionRepo;
        _context = context;
    }

    public async Task<PayrollSuggestionViewDto> CreateAsync(long creatorId, PayrollSuggestionCreateDto dto)
    {
        if (dto.Amount <= 0)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionInvalidData, "Số tiền kiến nghị phải lớn hơn 0");
        }

        var suggestion = new PayrollSuggestion
        {
            BranchId = dto.BranchId,
            AccountId = dto.AccountId,
            Month = dto.Month,
            Year = dto.Year,
            Title = dto.Title.Trim(),
            Type = dto.Type,
            Amount = dto.Amount,
            Reason = dto.Reason.Trim(),
            ApplyOption = string.IsNullOrWhiteSpace(dto.ApplyOption) ? "NextMonth" : dto.ApplyOption,
            IsResigned = dto.IsResigned,
            Status = PayrollSuggestionStatus.Pending,
            CreatedBy = creatorId,
            CreatedAt = DateTime.UtcNow
        };

        await _suggestionRepo.CreateAsync(suggestion);
        await _suggestionRepo.SaveChangesAsync();

        var reloaded = await _suggestionRepo.GetByIdAsync(suggestion.Id);
        return MapToViewDto(reloaded ?? suggestion);
    }

    public async Task<List<PayrollSuggestionViewDto>> GetFilteredAsync(PayrollSuggestionQueryDto query)
    {
        var list = await _suggestionRepo.GetFilteredAsync(query);
        return list.Select(MapToViewDto).ToList();
    }

    public async Task<PayrollSuggestionViewDto?> GetByIdAsync(long id)
    {
        var suggestion = await _suggestionRepo.GetByIdAsync(id);
        return suggestion != null ? MapToViewDto(suggestion) : null;
    }

    public async Task<PayrollSuggestionViewDto> ProcessAsync(long suggestionId, long managerId, PayrollSuggestionProcessDto dto)
    {
        var suggestion = await _suggestionRepo.GetByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionNotFound, $"Không tìm thấy kiến nghị lương #{suggestionId}");
        }

        if (!Enum.TryParse<PayrollSuggestionStatus>(dto.Status, true, out var newStatus))
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionInvalidData, "Trạng thái xử lý không hợp lệ (Approved | Rejected)");
        }

        suggestion.Status = newStatus;
        suggestion.ManagerNote = dto.ManagerNote?.Trim();
        suggestion.ProcessedBy = managerId;
        suggestion.ProcessedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.ForceApplyOption))
        {
            suggestion.ApplyOption = dto.ForceApplyOption;
        }

        if (newStatus == PayrollSuggestionStatus.Approved)
        {
            // Calculate target month and year
            int targetMonth = suggestion.Month;
            int targetYear = suggestion.Year;

            bool applyToCurrentMonth = suggestion.IsResigned || 
                                       suggestion.ApplyOption == "CurrentMonth" || 
                                       suggestion.ApplyOption == "ResignImmediate";

            if (!applyToCurrentMonth && suggestion.ApplyOption == "NextMonth")
            {
                targetMonth = (suggestion.Month % 12) + 1;
                targetYear = suggestion.Month == 12 ? suggestion.Year + 1 : suggestion.Year;
            }

            suggestion.Month = targetMonth;
            suggestion.Year = targetYear;

            // Try to find target payroll
            var targetPayroll = await _context.Payrolls
                .Include(p => p.SalaryDetails)
                .FirstOrDefaultAsync(p => p.BranchId == suggestion.BranchId &&
                                          p.AccountId == suggestion.AccountId &&
                                          p.Month == targetMonth &&
                                          p.Year == targetYear);

            if (targetPayroll != null)
            {
                var salaryDetail = new SalaryDetail
                {
                    PayrollId = targetPayroll.Id,
                    AccountId = suggestion.AccountId,
                    Title = $"[Kiến nghị] {suggestion.Title}",
                    Type = suggestion.Type,
                    Amount = suggestion.Amount,
                    Note = $"Từ kiến nghị #{suggestion.Id}: {suggestion.Reason}",
                    CreatedBy = managerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SalaryDetails.Add(salaryDetail);

                RecalculatePayrollTotals(targetPayroll);
                _context.Payrolls.Update(targetPayroll);

                suggestion.IsApplied = true;
                suggestion.AppliedPayrollId = targetPayroll.Id;
            }
        }

        await _suggestionRepo.UpdateAsync(suggestion);
        await _suggestionRepo.SaveChangesAsync();

        var reloaded = await _suggestionRepo.GetByIdAsync(suggestion.Id);
        return MapToViewDto(reloaded ?? suggestion);
    }

    public async Task<PayrollSuggestionViewDto> UpdateAsync(long suggestionId, long accountId, PayrollSuggestionUpdateDto dto)
    {
        var suggestion = await _suggestionRepo.GetByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionNotFound, $"Không tìm thấy kiến nghị lương #{suggestionId}");
        }

        if (suggestion.Status != PayrollSuggestionStatus.Pending)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionInvalidData, "Chỉ có thể chỉnh sửa kiến nghị ở trạng thái Chờ duyệt (Pending)");
        }

        if (dto.Amount <= 0)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionInvalidData, "Số tiền kiến nghị phải lớn hơn 0");
        }

        suggestion.Title = dto.Title.Trim();
        suggestion.Type = dto.Type;
        suggestion.Amount = dto.Amount;
        suggestion.Reason = dto.Reason.Trim();
        suggestion.ApplyOption = string.IsNullOrWhiteSpace(dto.ApplyOption) ? "NextMonth" : dto.ApplyOption;
        suggestion.IsResigned = dto.IsResigned;

        await _suggestionRepo.UpdateAsync(suggestion);
        await _suggestionRepo.SaveChangesAsync();

        var reloaded = await _suggestionRepo.GetByIdAsync(suggestion.Id);
        return MapToViewDto(reloaded ?? suggestion);
    }

    public async Task DeleteAsync(long suggestionId, long accountId)
    {
        var suggestion = await _suggestionRepo.GetByIdAsync(suggestionId);
        if (suggestion == null)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionNotFound, $"Không tìm thấy kiến nghị lương #{suggestionId}");
        }

        if (suggestion.Status != PayrollSuggestionStatus.Pending)
        {
            throw new MenuGoException(ErrorCodes.PayrollSuggestionInvalidData, "Chỉ có thể xóa kiến nghị ở trạng thái Chờ duyệt (Pending)");
        }

        await _suggestionRepo.DeleteAsync(suggestion);
        await _suggestionRepo.SaveChangesAsync();
    }

    public async Task ApplyApprovedSuggestionsToPayrollAsync(long payrollId)
    {
        var payroll = await _context.Payrolls
            .Include(p => p.SalaryDetails)
            .FirstOrDefaultAsync(p => p.Id == payrollId);

        if (payroll == null) return;

        var unappliedSuggestions = await _suggestionRepo.GetApprovedUnappliedForMonthAsync(
            payroll.BranchId, payroll.AccountId, payroll.Month, payroll.Year);

        if (!unappliedSuggestions.Any()) return;

        bool updated = false;
        foreach (var sug in unappliedSuggestions)
        {
            var detail = new SalaryDetail
            {
                PayrollId = payroll.Id,
                AccountId = payroll.AccountId,
                Title = $"[Kiến nghị] {sug.Title}",
                Type = sug.Type,
                Amount = sug.Amount,
                Note = $"Từ kiến nghị #{sug.Id}: {sug.Reason}",
                CreatedBy = sug.ProcessedBy ?? sug.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.SalaryDetails.Add(detail);

            sug.IsApplied = true;
            sug.AppliedPayrollId = payroll.Id;
            _context.PayrollSuggestions.Update(sug);
            updated = true;
        }

        if (updated)
        {
            RecalculatePayrollTotals(payroll);
            _context.Payrolls.Update(payroll);
            await _context.SaveChangesAsync();
        }
    }

    private static void RecalculatePayrollTotals(Payroll payroll)
    {
        if (payroll.SalaryDetails == null) return;

        payroll.TotalAllowance = payroll.SalaryDetails
            .Where(sd => sd.Type == SalaryAdjustmentType.Allowance)
            .Sum(sd => sd.Amount);

        payroll.BonusAmount = payroll.SalaryDetails
            .Where(sd => sd.Type == SalaryAdjustmentType.Bonus)
            .Sum(sd => sd.Amount);

        payroll.TotalDeduction = payroll.SalaryDetails
            .Where(sd => sd.Type == SalaryAdjustmentType.Deduction)
            .Sum(sd => sd.Amount);

        payroll.PenaltyAmount = payroll.SalaryDetails
            .Where(sd => sd.Type == SalaryAdjustmentType.Penalty)
            .Sum(sd => sd.Amount);

        payroll.NetSalary = payroll.CalculatedSalary + payroll.TotalAllowance + payroll.BonusAmount - payroll.TotalDeduction - payroll.PenaltyAmount;
    }

    private static PayrollSuggestionViewDto MapToViewDto(PayrollSuggestion s)
    {
        var acc = s.Account;
        return new PayrollSuggestionViewDto
        {
            Id = s.Id,
            BranchId = s.BranchId,
            BranchName = s.Branch?.Name ?? string.Empty,
            AccountId = s.AccountId,
            EmployeeName = acc?.Name ?? string.Empty,
            EmployeeCode = acc != null ? $"NV{acc.Id:D4}" : string.Empty,
            Month = s.Month,
            Year = s.Year,
            Title = s.Title,
            Type = s.Type,
            TypeName = s.Type.ToString(),
            Amount = s.Amount,
            Reason = s.Reason,
            ApplyOption = s.ApplyOption,
            IsResigned = s.IsResigned,
            Status = s.Status.ToString(),
            ManagerNote = s.ManagerNote,
            ProcessedBy = s.ProcessedBy,
            ProcessorName = s.Processor?.Name,
            ProcessedAt = s.ProcessedAt,
            IsApplied = s.IsApplied,
            AppliedPayrollId = s.AppliedPayrollId,
            CreatedBy = s.CreatedBy,
            CreatorName = s.Creator?.Name ?? string.Empty,
            CreatedAt = s.CreatedAt
        };
    }
}
