using MenuGoBE.Dtos.Payroll;

namespace MenuGoBE.Interface.Services;

public interface IPayrollSuggestionService
{
    Task<PayrollSuggestionViewDto> CreateAsync(long creatorId, PayrollSuggestionCreateDto dto);
    Task<List<PayrollSuggestionViewDto>> GetFilteredAsync(PayrollSuggestionQueryDto query);
    Task<PayrollSuggestionViewDto?> GetByIdAsync(long id);
    Task<PayrollSuggestionViewDto> ProcessAsync(long suggestionId, long managerId, PayrollSuggestionProcessDto dto);
    Task<PayrollSuggestionViewDto> UpdateAsync(long suggestionId, long accountId, PayrollSuggestionUpdateDto dto);
    Task DeleteAsync(long suggestionId, long accountId);
    Task ApplyApprovedSuggestionsToPayrollAsync(long payrollId);
}
