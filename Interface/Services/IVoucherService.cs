using MenuGoBE.Dtos.Voucher;

namespace MenuGoBE.Interface.Services;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync();
    Task<VoucherDto?> GetByIdAsync(long id);
    Task<VoucherDto> CreateAsync(VoucherCreateDto dto);
    Task<bool> UpdateAsync(VoucherUpdateDto dto);
    Task<bool> DeleteAsync(long id);
    
    Task<VoucherCheckResponseDto> CheckVoucherAsync(string code, long branchId, decimal totalOrderAmount);
}
