using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IVoucherRepository
{
    Task<List<Voucher>> GetAllAsync();
    Task<List<Voucher>> GetByBranchIdAsync(long branchId);
    Task<Voucher?> GetByIdAsync(long id);
    Task<Voucher?> GetByCodeAsync(string code);
    Task<Voucher> CreateAsync(Voucher voucher);
    Task<bool> UpdateAsync(Voucher voucher);
    Task<bool> DeleteAsync(long id);
    Task<int> GetUsageCountAsync(long voucherId, long customerId);
    Task RecordUsageAsync(VoucherUsage usage);
    Task<List<Voucher>> GetAvailableAsync(long? customerId = null);
}
