using MenuGoBE.Dtos.Voucher;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Data;

namespace MenuGoBE.Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _repo;
    private readonly ICustomerRepository _customerRepo;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

    public VoucherService(
        IVoucherRepository repo, 
        ICustomerRepository customerRepo,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _scopeFactory = scopeFactory;
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
            IsActive = v.IsActive,
            MaxUsagePerCustomer = v.MaxUsagePerCustomer
        }).ToList();
    }

    public async Task<List<VoucherDto>> GetByBranchIdAsync(long branchId)
    {
        var list = await _repo.GetByBranchIdAsync(branchId);
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
            IsActive = v.IsActive,
            MaxUsagePerCustomer = v.MaxUsagePerCustomer
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
            IsActive = v.IsActive,
            MaxUsagePerCustomer = v.MaxUsagePerCustomer
        };
    }

    public async Task<VoucherDto> CreateAsync(VoucherCreateDto dto)
    {
        var existing = await _repo.GetByCodeAsync(dto.Code);
        if (existing != null) throw new Exception("Mã Voucher đã tồn tại.");

        if (dto.DiscountValue < 0)
        {
            throw new Exception("Giá trị giảm giá không được âm.");
        }

        if (dto.DiscountType == "Percentage" && dto.DiscountValue > 100)
        {
            throw new Exception("Giá trị giảm giá theo phần trăm không được vượt quá 100%.");
        }

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
            IsActive = dto.IsActive,
            MaxUsagePerCustomer = dto.MaxUsagePerCustomer
        };

        var created = await _repo.CreateAsync(v);

        // Send emails
        if (created.IsActive)
        {
            SendVoucherEmailInBackground(created, true, false);
        }

        return await GetByIdAsync(created.Id) ?? throw new Exception("Tạo Voucher thất bại");
    }

    public async Task<bool> UpdateAsync(VoucherUpdateDto dto)
    {
        var v = await _repo.GetByIdAsync(dto.Id);
        if (v == null) return false;

        var existing = await _repo.GetByCodeAsync(dto.Code);
        if (existing != null && existing.Id != dto.Id)
            throw new Exception("Mã Voucher đã tồn tại ở chương trình khác.");

        if (dto.DiscountValue < 0)
        {
            throw new Exception("Giá trị giảm giá không được âm.");
        }

        if (dto.DiscountType == "Percentage" && dto.DiscountValue > 100)
        {
            throw new Exception("Giá trị giảm giá theo phần trăm không được vượt quá 100%.");
        }

        if (v.UsedCount > 0)
        {
            if (v.Code != dto.Code.ToUpper() ||
                v.DiscountType != dto.DiscountType ||
                v.DiscountValue != dto.DiscountValue ||
                v.MaxDiscount != dto.MaxDiscount ||
                v.MinOrderValue != dto.MinOrderValue ||
                v.BranchId != dto.BranchId)
            {
                throw new Exception("Voucher đã được khách hàng sử dụng. Không thể thay đổi mã voucher hoặc các thông số tài chính.");
            }
        }

        bool wasActive = v.IsActive;

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
        v.MaxUsagePerCustomer = dto.MaxUsagePerCustomer;

        var result = await _repo.UpdateAsync(v);

        if (result && v.IsActive)
        {
            bool justEnabled = !wasActive && v.IsActive;
            SendVoucherEmailInBackground(v, false, justEnabled);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v != null && v.UsedCount > 0)
        {
            throw new Exception("Không thể xóa: Voucher này đã được khách hàng sử dụng. Để ngừng áp dụng, vui lòng chọn Sửa và tắt trạng thái Hoạt động.");
        }
        return await _repo.DeleteAsync(id);
    }

    public async Task<VoucherCheckResponseDto> CheckVoucherAsync(string code, long branchId, decimal totalOrderAmount, long? customerId = null)
    {
        var v = await _repo.GetByCodeAsync(code);
        if (v == null)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không tồn tại." };

        if (!v.IsActive)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher đã bị khóa." };

        // Kiểm tra khách vãng lai (không đăng nhập) có dùng voucher không
        // Nếu policy là bắt buộc khách hàng phải đăng nhập mới được dùng voucher:
        if (!customerId.HasValue)
        {
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher chỉ áp dụng cho khách hàng thành viên. Vui lòng đăng nhập." };
        }

        var now = DateTime.UtcNow;
        if (now < v.StartDate || now > v.EndDate)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không trong thời gian áp dụng." };

        if (v.UsedCount >= v.Quantity)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher đã hết lượt sử dụng." };

        if (v.BranchId.HasValue && v.BranchId.Value > 0 && branchId > 0 && v.BranchId.Value != branchId)
            return new VoucherCheckResponseDto { IsValid = false, Message = "Voucher không áp dụng tại chi nhánh này." };

        // Kiểm tra giới hạn per-customer
        if (v.MaxUsagePerCustomer.HasValue)
        {
            var usageCount = await _repo.GetUsageCountAsync(v.Id, customerId.Value);
            
            if (usageCount >= v.MaxUsagePerCustomer.Value)
            {
                return new VoucherCheckResponseDto { IsValid = false, Message = $"Voucher đã vượt quá giới hạn sử dụng ({v.MaxUsagePerCustomer.Value} lần) cho tài khoản của bạn." };
            }
        }

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

    public async Task RecordVoucherUsageAsync(long voucherId, long customerId, long? orderId)
    {
        var usage = new VoucherUsage
        {
            VoucherId = voucherId,
            CustomerId = customerId,
            OrderId = orderId,
            UsedAt = DateTime.UtcNow
        };
        await _repo.RecordUsageAsync(usage);
        
        // Also update voucher used count
        var v = await _repo.GetByIdAsync(voucherId);
        if (v != null)
        {
            v.UsedCount++;
            await _repo.UpdateAsync(v);
        }
    }

    public async Task<List<VoucherDto>> GetAvailableAsync(long? customerId = null)
    {
        var available = await _repo.GetAvailableAsync(customerId);

        return available.Select(v => new VoucherDto
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MinOrderValue = v.MinOrderValue,
            MaxDiscount = v.MaxDiscount,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            IsActive = v.IsActive,
            Quantity = v.Quantity,
            UsedCount = v.UsedCount,
            MaxUsagePerCustomer = v.MaxUsagePerCustomer,
            BranchId = v.BranchId,
            BranchName = v.Branch?.Name
        }).ToList();
    }

    private void SendVoucherEmailInBackground(Voucher v, bool isNew, bool justEnabled)
    {
        // Fire and forget task to send emails
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
                var branchRepo = scope.ServiceProvider.GetRequiredService<IBranchRepository>();

                var subscribers = await customerRepo.GetPromoEmailSubscribersAsync();
                if (subscribers.Count == 0) return;

                string branchName = "Toàn hệ thống";
                if (v.BranchId.HasValue && v.BranchId.Value > 0)
                {
                    var branch = await branchRepo.GetByIdAsync(v.BranchId.Value);
                    if (branch != null) branchName = branch.Name;
                }

                string subject;
                if (isNew) subject = $"[MenuGo] Thông báo mã giảm giá mới: {v.Name}";
                else if (justEnabled) subject = $"[MenuGo] Mã giảm giá {v.Name} đã được kích hoạt lại";
                else subject = $"[MenuGo] Cập nhật ưu đãi mã giảm giá: {v.Name}";
                
                var discountText = v.DiscountType == "Percentage" 
                    ? $"{v.DiscountValue}% (Tối đa {v.MaxDiscount:N0}đ)" 
                    : $"{v.DiscountValue:N0}đ";

                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                        <div style='background-color: #3b82f6; color: white; padding: 25px 20px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 24px;'>Quà tặng từ MenuGo</h1>
                            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>MenuGo gửi tặng bạn mã giảm giá</p>
                        </div>
                        
                        <div style='padding: 30px 20px;'>
                            <h2 style='color: #0f172a; margin-top: 0; font-size: 20px; text-align: center;'>{v.Name}</h2>
                            <p style='color: #475569; font-size: 15px; line-height: 1.6; text-align: center;'>
                                Nhập mã dưới đây khi đặt món tại MenuGo để nhận ưu đãi:
                            </p>
                            
                            <div style='background: #fffbeb; border: 2px dashed #f59e0b; border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;'>
                                <span style='display: block; color: #b45309; font-size: 14px; margin-bottom: 8px; font-weight: bold;'>Mã khuyến mãi của bạn</span>
                                <div style='font-size: 28px; font-weight: 900; color: #ea580c; letter-spacing: 2px;'>{v.Code}</div>
                            </div>

                            <div style='background-color: #f8fafc; border-radius: 8px; padding: 20px; margin: 20px 0;'>
                                <h3 style='color: #0f172a; font-size: 16px; margin: 0 0 15px 0; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px;'>Chi Tiết Ưu Đãi</h3>
                                <div style='margin-bottom: 12px; display: flex; align-items: flex-start;'>
                                    <strong style='width: 150px; color: #475569; flex-shrink: 0;'>Mức giảm:</strong> 
                                    <span style='color: #ef4444; font-weight: bold;'>{discountText}</span>
                                </div>
                                <div style='margin-bottom: 12px; display: flex; align-items: flex-start;'>
                                    <strong style='width: 150px; color: #475569; flex-shrink: 0;'>Đơn tối thiểu:</strong> 
                                    <span style='color: #0f172a; font-weight: 500;'>{v.MinOrderValue:N0}đ</span>
                                </div>
                                <div style='margin-bottom: 12px; display: flex; align-items: flex-start;'>
                                    <strong style='width: 150px; color: #475569; flex-shrink: 0;'>Áp dụng tại:</strong> 
                                    <span style='color: #0f172a; font-weight: 500;'>{branchName}</span>
                                </div>
                                <div style='margin-bottom: 12px; display: flex; align-items: flex-start;'>
                                    <strong style='width: 150px; color: #475569; flex-shrink: 0;'>Hạn mức:</strong> 
                                    <span style='color: #0f172a; font-weight: 500;'>{(v.MaxUsagePerCustomer.HasValue ? $"{v.MaxUsagePerCustomer.Value} lần/khách" : "Không giới hạn")}</span>
                                </div>
                                <div style='display: flex; align-items: flex-start;'>
                                    <strong style='width: 150px; color: #475569; flex-shrink: 0;'>Thời gian:</strong> 
                                    <span style='color: #0f172a; font-weight: 500;'>{v.StartDate:dd/MM/yyyy HH:mm} - {v.EndDate:dd/MM/yyyy HH:mm}</span>
                                </div>
                            </div>
                        </div>
                        
                        <div style='background-color: #f1f5f9; padding: 15px 20px; text-align: center; color: #64748b; font-size: 12px;'>
                            <p style='margin: 0;'>Email này được gửi tự động từ hệ thống MenuGo.</p>
                            <p style='margin: 5px 0 0 0;'>Cảm ơn bạn đã luôn đồng hành cùng chúng tôi!</p>
                        </div>
                    </div>
                ";

                foreach (var customer in subscribers)
                {
                    try
                    {
                        await emailService.SendEmailAsync(customer.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending voucher email to {customer.Email}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in background email task: {ex.Message}");
            }
        });
    }
}
