using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services
{
    public interface IPayrollService
    {
        Task<List<PayrollViewDto>> GetAllAsync(long? branchId = null, int? month = null, int? year = null, long? accountId = null, List<long>? roleIds = null);
        Task<PayrollViewDto?> GetByIdAsync(long id);
        Task<List<PayrollViewDto>> GetByAccountIdAsync(long accountId, int? month = null, int? year = null);
        Task<PayrollViewDto> CreateAsync(PayrollCreateDto dto);
        Task<bool> UpdateAsync(PayrollUpdateDto dto);
        Task<bool> DeleteAsync(long id);
        Task<bool> UpdateStatusAsync(long id, PayrollStatus status, DateTime? paymentDate = null);
        Task<bool> LockPayrollAsync(long id, long userId);
        Task<bool> UnlockPayrollAsync(long id, long userId);
        Task<bool> ApprovePayrollAsync(long id, long userId);
        Task<PayrollViewDto> GenerateEmployeePayrollAsync(PayrollGenerateDto dto);
        Task<List<PayrollViewDto>> GenerateBatchPayrollAsync(PayrollBatchGenerateDto dto, bool isAdmin, bool isManager, List<long> managerBranchIds);
    }
}
