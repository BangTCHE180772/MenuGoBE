using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;

    public VoucherService(IVoucherRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<VoucherDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(v => new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscount = v.MaxDiscount,
            MinOrderValue = v.MinOrderValue,
            BranchId = v.BranchId,
            BranchName = v.Branch?.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            Quantity = v.Quantity,
            UsedCount = v.UsedCount,
            IsActive = v.IsActive
        }).ToList();
    }

    public async Task<VoucherDto?> GetByIdAsync(long id)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v == null) return null;

        return new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscount = v.MaxDiscount,
            MinOrderValue = v.MinOrderValue,
            BranchId = v.BranchId,
            BranchName = v.Branch?.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            Quantity = v.Quantity,
            UsedCount = v.UsedCount,
            IsActive = v.IsActive
        };
    }

    public async Task<VoucherDto> CreateAsync(VoucherCreateDto dto)
    {
        var existing = await _repo.GetByCodeAsync(dto.Code);
        if (existing != null) throw new Exception("Mã Voucher đã tồn tại.");

        var v = new Voucher
        {
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MaxDiscount = dto.MaxDiscount,
            MinOrderValue = dto.MinOrderValue,
            BranchId = dto.BranchId,
            StartDate = dto.StartDate.ToUniversalTime(),
            EndDate = dto.EndDate.ToUniversalTime(),
            Quantity = dto.Quantity,
            UsedCount = 0,
            IsActive = dto.IsActive
        };

        var created = await _repo.CreateAsync(v);
        return await GetByIdAsync(created.Id) ?? throw new Exception("Tạo Voucher thất bại");
    }

    public async Task<bool> UpdateAsync(VoucherUpdateDto dto)
    {
        var v = await _repo.GetByIdAsync(dto.Id);
        if (v == null) return false;

        var existing = await _repo.GetByCodeAsync(dto.Code);
        if (existing != null && existing.Id != dto.Id)
            throw new Exception("Mã Voucher đã tồn tại ở chương trình khác.");

        v.Code = dto.Code.ToUpper();
        v.Name = dto.Name;
        v.DiscountType = dto.DiscountType;
        v.DiscountValue = dto.DiscountValue;
        v.MaxDiscount = dto.MaxDiscount;
        v.MinOrderValue = dto.MinOrderValue;
        v.BranchId = dto.BranchId;
        v.StartDate = dto.StartDate.ToUniversalTime();
        v.EndDate = dto.EndDate.ToUniversalTime();
        v.Quantity = dto.Quantity;
        v.IsActive = dto.IsActive;

        return await _repo.UpdateAsync(v);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _repo.DeleteAsync(id);
    }

    public async Task<VoucherCheckResponseDto> CheckVoucherAsync(string code, long branchId, decimal totalOrderAmount)
    {
        var v = await _repo.GetByCodeAsync(code);
        if (v == null)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không tồn tại." };

        if (!v.IsActive)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher đã bị khóa." };

        var now = DateTime.UtcNow;
        if (now < v.StartDate || now > v.EndDate)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không trong thời gian áp dụng." };

        if (v.UsedCount >= v.Quantity)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher đã hết lượt sử dụng." };

        if (v.BranchId.HasValue && v.BranchId.Value != branchId)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không áp dụng tại chi nhánh này." };

        if (totalOrderAmount < v.MinOrderValue)
            return new VoucherCheckResponseDto { IsValid = false, Message = $"Đơn hàng chưa đạt giá trị tối thiểu ({v.MinOrderValue:N0}đ) để áp dụng." };

        decimal discountAmount = 0;
        if (v.DiscountType == "Fixed")
        {
            discountAmount = v.DiscountValue;
        }
        else if (v.DiscountType == "Percentage")
        {
            discountAmount = totalOrderAmount * (v.DiscountValue / 100);
            if (v.MaxDiscount > 0 && discountAmount > v.MaxDiscount)
            {
                discountAmount = v.MaxDiscount;
            }
        }

        if (discountAmount > totalOrderAmount)
        {
            discountAmount = totalOrderAmount;
        }

        return new VoucherCheckResponseDto
        {
            IsValid = true,
            Message = "Áp dụng Voucher thành công.",
            DiscountAmount = discountAmount,
            Voucher = v
        };
    }
}
