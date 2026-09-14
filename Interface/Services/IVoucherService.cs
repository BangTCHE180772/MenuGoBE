using MenuGoBE.Dtos.Voucher;

namespace MenuGoBE.Interface.Services;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync();
    Task<List<VoucherDto>> GetByBranchIdAsync(long branchId);
    Task<VoucherDto?> GetByIdAsync(long id);
    Task<VoucherDto> CreateAsync(VoucherCreateDto dto);
    Task<bool> UpdateAsync(VoucherUpdateDto dto);
    Task<bool> DeleteAsync(long id);
    
    Task<VoucherCheckResponseDto> CheckVoucherAsync(string code, long branchId, decimal totalOrderAmount, long? customerId = null);
    Task RecordVoucherUsageAsync(long voucherId, long customerId, long? orderId);
    Task<List<VoucherDto>> GetAvailableAsync(long? customerId = null);
}
