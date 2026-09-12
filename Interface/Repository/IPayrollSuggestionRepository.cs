using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository;

public interface IPayrollSuggestionRepository
{
    Task<PayrollSuggestion?> GetByIdAsync(long id);
    Task<List<PayrollSuggestion>> GetFilteredAsync(PayrollSuggestionQueryDto query);
    Task<List<PayrollSuggestion>> GetApprovedUnappliedForMonthAsync(long branchId, long accountId, int month, int year);
    Task CreateAsync(PayrollSuggestion entity);
    Task UpdateAsync(PayrollSuggestion entity);
    Task DeleteAsync(PayrollSuggestion entity);
    Task SaveChangesAsync();
}
