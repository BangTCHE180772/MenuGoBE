using MenuGoBE.Data;
using MenuGoBE.Dtos.Promotion;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Repositories;
using MenuGoBE.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MenuGoBE.Service;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepo;
    private readonly AppDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;

    public PromotionService(
        IPromotionRepository promotionRepo,
        AppDbContext context,
        IServiceScopeFactory scopeFactory)
    {
        _promotionRepo = promotionRepo;
        _context = context;
        _scopeFactory = scopeFactory;
    }

    public async Task<PromotionDto> CreatePromotionAsync(PromotionCreateDto dto, long userId)
    {
        // 1. Validation
        if (dto.StartDate >= dto.EndDate)
        {
            throw new InvalidOperationException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        if (dto.StartDate < DateTime.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Thời gian bắt đầu không được ở trong quá khứ.");
        }

        if (dto.Scope == PromotionScope.SpecificBranches && (dto.BranchIds == null || !dto.BranchIds.Any()))
        {
            throw new InvalidOperationException("Vui lòng chọn ít nhất 1 chi nhánh áp dụng.");
        }

        if (dto.DiscountValue < 0)
        {
            throw new InvalidOperationException("Giá trị giảm giá không được âm.");
        }

        if (dto.DiscountType == "Percentage" && dto.DiscountValue > 100)
        {
            throw new InvalidOperationException("Giá trị giảm giá theo phần trăm không được vượt quá 100%.");
        }

        if (dto.ProductIds == null || !dto.ProductIds.Any())
        {
            throw new InvalidOperationException("Vui lòng chọn ít nhất 1 sản phẩm áp dụng.");
        }

        foreach (var productId in dto.ProductIds)
        {
            long? targetBranchId = dto.Scope == PromotionScope.SpecificBranches ? dto.BranchIds.First() : null;
            bool hasOverlap = await _promotionRepo.HasOverlappingPromotionAsync(productId, targetBranchId, dto.StartDate, dto.EndDate);
            if (hasOverlap)
            {
                var p = await _context.Products.FindAsync(productId);
                throw new InvalidOperationException($"Sản phẩm {p?.Name} đang có chương trình khuyến mãi khác trong thời gian này.");
            }
        }

        // 2. Map Entity
        var promotion = new Promotion
        {
            Name = dto.Name,
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MaxDiscount = dto.MaxDiscount,
            Scope = dto.Scope,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            PromotionProducts = dto.ProductIds.Select(p => new PromotionProduct { ProductId = p }).ToList()
        };

        if (dto.Scope == PromotionScope.SpecificBranches)
        {
            promotion.PromotionBranches = dto.BranchIds.Select(b => new PromotionBranch { BranchId = b }).ToList();
        }

        await _promotionRepo.CreatePromotionAsync(promotion);

        var savedPromotion = await MapToDtoAsync(await _promotionRepo.GetPromotionByIdAsync(promotion.Id));
        
        if (savedPromotion.IsActive)
        {
            SendPromotionEmailInBackground(savedPromotion, true, false);
        }

        return savedPromotion;
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(long id)
    {
        var promotion = await _promotionRepo.GetPromotionByIdAsync(id);
        return promotion == null ? null : await MapToDtoAsync(promotion);
    }

    public async Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync()
    {
        var promotions = await _promotionRepo.GetAllPromotionsAsync();
        var dtos = new List<PromotionDto>();
        foreach (var p in promotions)
        {
            dtos.Add(await MapToDtoAsync(p));
        }
        return dtos;
    }

    public async Task<IEnumerable<PromotionDto>> GetAllPromotionsByBranchAsync(long branchId)
    {
        var promotions = await _promotionRepo.GetAllPromotionsByBranchAsync(branchId);
        var dtos = new List<PromotionDto>();
        foreach (var p in promotions)
        {
            dtos.Add(await MapToDtoAsync(p));
        }
        return dtos;
    }

    public async Task<PromotionDto> UpdatePromotionAsync(PromotionUpdateDto dto, long userId)
    {
        var existing = await _promotionRepo.GetPromotionByIdAsync(dto.Id);
        if (existing == null) throw new InvalidOperationException("Không tìm thấy khuyến mãi.");

        if (dto.DiscountValue < 0)
        {
            throw new InvalidOperationException("Giá trị giảm giá không được âm.");
        }

        if (dto.DiscountType == "Percentage" && dto.DiscountValue > 100)
        {
            throw new InvalidOperationException("Giá trị giảm giá theo phần trăm không được vượt quá 100%.");
        }

        // Validation overlap
        foreach (var productId in dto.ProductIds)
        {
            long? targetBranchId = dto.Scope == PromotionScope.SpecificBranches ? dto.BranchIds.First() : null;
            bool hasOverlap = await _promotionRepo.HasOverlappingPromotionAsync(productId, targetBranchId, dto.StartDate, dto.EndDate, dto.Id);
            if (hasOverlap)
            {
                var p = await _context.Products.FindAsync(productId);
                throw new InvalidOperationException($"Sản phẩm {p?.Name} đang có chương trình khuyến mãi khác trong thời gian này.");
            }
        }

        var hasBeenUsed = await _context.OrderDetails.AnyAsync(od => od.PromotionId == dto.Id);
        if (hasBeenUsed)
        {
            if (existing.DiscountType != dto.DiscountType ||
                existing.DiscountValue != dto.DiscountValue ||
                existing.MaxDiscount != dto.MaxDiscount ||
                existing.Scope != dto.Scope)
            {
                throw new InvalidOperationException("Khuyến mãi đã được khách hàng sử dụng. Không thể thay đổi các thông số tài chính.");
            }

            bool productsChanged = existing.PromotionProducts.Count != dto.ProductIds.Count || 
                                   existing.PromotionProducts.Any(pp => !dto.ProductIds.Contains(pp.ProductId));
            if (productsChanged) throw new InvalidOperationException("Khuyến mãi đã được sử dụng. Không thể thay đổi danh sách món ăn.");
            
            if (dto.Scope == PromotionScope.SpecificBranches) {
               bool branchesChanged = existing.PromotionBranches.Count != dto.BranchIds.Count ||
                                      existing.PromotionBranches.Any(pb => !dto.BranchIds.Contains(pb.BranchId));
               if (branchesChanged) throw new InvalidOperationException("Khuyến mãi đã được sử dụng. Không thể thay đổi chi nhánh áp dụng.");
            }
        }

        bool wasActive = existing.IsActive;

        // Cập nhật thông tin cơ bản
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.DiscountType = dto.DiscountType;
        existing.DiscountValue = dto.DiscountValue;
        existing.MaxDiscount = dto.MaxDiscount;
        existing.Scope = dto.Scope;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.IsActive = dto.IsActive;

        // Xóa quan hệ cũ trước
        _context.PromotionProducts.RemoveRange(existing.PromotionProducts);
        _context.PromotionBranches.RemoveRange(existing.PromotionBranches);
        await _context.SaveChangesAsync();

        // Xóa khỏi collection trong memory để tránh bị tracking conflict
        existing.PromotionProducts.Clear();
        existing.PromotionBranches.Clear();

        // Thêm quan hệ mới
        foreach(var pId in dto.ProductIds)
        {
            existing.PromotionProducts.Add(new PromotionProduct { ProductId = pId, PromotionId = dto.Id });
        }

        if (dto.Scope == PromotionScope.SpecificBranches)
        {
            foreach(var bId in dto.BranchIds)
            {
                existing.PromotionBranches.Add(new PromotionBranch { BranchId = bId, PromotionId = dto.Id });
            }
        }

        await _context.SaveChangesAsync();

        var updatedPromotion = await MapToDtoAsync(await _promotionRepo.GetPromotionByIdAsync(dto.Id));

        if (updatedPromotion.IsActive)
        {
            bool justEnabled = !wasActive && updatedPromotion.IsActive;
            SendPromotionEmailInBackground(updatedPromotion, false, justEnabled);
        }

        return updatedPromotion;
    }

    public async Task DeletePromotionAsync(long id)
    {
        var hasBeenUsed = await _context.OrderDetails.AnyAsync(od => od.PromotionId == id);
        if (hasBeenUsed)
        {
            throw new InvalidOperationException("Không thể xóa: Chương trình khuyến mãi này đã được khách hàng sử dụng trong giao dịch. Để ngừng áp dụng, vui lòng chọn Sửa và tắt trạng thái Hoạt động.");
        }

        await _promotionRepo.DeletePromotionAsync(id);
    }

    public async Task RemoveProductFromPromotionAsync(long promoId, long productId)
    {
        var pp = await _context.PromotionProducts
            .FirstOrDefaultAsync(x => x.PromotionId == promoId && x.ProductId == productId);
        if (pp == null) throw new InvalidOperationException("Sản phẩm không nằm trong chương trình khuyến mãi này.");

        _context.PromotionProducts.Remove(pp);
        await _context.SaveChangesAsync();

        // Nếu KM không còn sản phẩm nào → xóa luôn KM
        var remaining = await _context.PromotionProducts.CountAsync(x => x.PromotionId == promoId);
        if (remaining == 0)
        {
            await _promotionRepo.DeletePromotionAsync(promoId);
        }
    }

    public async Task<Dictionary<long, PromotionDto>> GetActivePromotionMapForBranchAsync(long branchId)
    {
        var activePromotions = await _promotionRepo.GetActivePromotionsForBranchAsync(branchId);
        
        var map = new Dictionary<long, PromotionDto>();
        foreach (var p in activePromotions)
        {
            var dto = await MapToDtoAsync(p);
            
            // Nếu không có sản phẩm nào cụ thể, áp dụng cho tất cả (key = 0)
            if (p.PromotionProducts == null || p.PromotionProducts.Count == 0)
            {
                map[0] = dto;
            }
            else
            {
                foreach (var pp in p.PromotionProducts)
                {
                    map[pp.ProductId] = dto;
                }
            }
        }
        
        return map;
    }

    private async Task<PromotionDto> MapToDtoAsync(Promotion p)
    {
        var now = DateTime.UtcNow;
        string status = p.EndDate < now ? "Expired" :
                       p.IsActive == false ? "Disabled" :
                       p.StartDate > now ? "Upcoming" : "Active";

        bool hasBeenUsed = await _context.OrderDetails.AnyAsync(od => od.PromotionId == p.Id);

        return new PromotionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            DiscountType = p.DiscountType,
            DiscountValue = p.DiscountValue,
            MaxDiscount = p.MaxDiscount,
            Scope = p.Scope,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive,
            Status = status,
            HasBeenUsed = hasBeenUsed,
            Products = p.PromotionProducts?.Select(pp => new PromotionProductDto
            {
                ProductId = pp.ProductId,
                ProductName = pp.Product?.Name ?? "",
                ProductCode = pp.Product?.SKUCode ?? "",
                OriginalPrice = pp.Product?.SellPrice ?? 0,
                ImageUrl = pp.Product?.Image?.ImageLink
            }).ToList() ?? new List<PromotionProductDto>(),
            Branches = p.PromotionBranches?.Select(pb => new PromotionBranchDto
            {
                BranchId = pb.BranchId,
                BranchName = pb.Branch?.Name ?? ""
            }).ToList() ?? new List<PromotionBranchDto>()
        };
    }
    private void SendPromotionEmailInBackground(PromotionDto savedPromotion, bool isNew, bool justEnabled)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

                var subscribers = await customerRepo.GetPromoEmailSubscribersAsync();

                if (!subscribers.Any()) return;

                string subject;
                if (isNew) subject = $"[MenuGo] Thông báo khuyến mãi mới: {savedPromotion.Name}";
                else if (justEnabled) subject = $"[MenuGo] Khuyến mãi {savedPromotion.Name} đã hoạt động trở lại";
                else subject = $"[MenuGo] Cập nhật thông tin khuyến mãi: {savedPromotion.Name}";
                
                string branchName = (savedPromotion.Scope == PromotionScope.AllBranches || savedPromotion.Scope.ToString() == "AllBranches" || !savedPromotion.Branches.Any()) 
                    ? "Tất cả chi nhánh" 
                    : string.Join(", ", savedPromotion.Branches.Select(b => b.BranchName));

                string discountText = savedPromotion.DiscountType == "Percentage" 
                    ? $"{savedPromotion.DiscountValue}%" 
                    : $"{savedPromotion.DiscountValue:N0}đ";

                var productItemsHtml = string.Join("", savedPromotion.Products.Select(p => {
                    decimal newPrice = savedPromotion.DiscountType == "Percentage"
                        ? p.OriginalPrice * (1 - savedPromotion.DiscountValue / 100m)
                        : Math.Max(0, p.OriginalPrice - savedPromotion.DiscountValue);
                    
                    if (savedPromotion.DiscountType == "Percentage" && savedPromotion.MaxDiscount > 0)
                    {
                        decimal discountAmt = p.OriginalPrice * (savedPromotion.DiscountValue / 100m);
                        if (discountAmt > savedPromotion.MaxDiscount)
                        {
                            newPrice = p.OriginalPrice - savedPromotion.MaxDiscount;
                        }
                    }

                    return $@"
                        <div style='display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px dashed #e2e8f0;'>
                            <div style='flex: 1; padding-right: 15px;'>
                                <h4 style='margin: 0 0 5px 0; color: #0f172a; font-size: 16px;'>{p.ProductName}</h4>
                                <span style='background-color: #fef2f2; color: #ef4444; padding: 2px 6px; border-radius: 4px; font-size: 12px; font-weight: bold;'>Giảm {discountText}</span>
                            </div>
                            <div style='text-align: right;'>
                                <div style='text-decoration: line-through; color: #94a3b8; font-size: 13px;'>{p.OriginalPrice:N0}đ</div>
                                <div style='color: #ea580c; font-weight: bold; font-size: 16px;'>{newPrice:N0}đ</div>
                            </div>
                        </div>";
                }));

                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                        <div style='background-color: #ea580c; color: white; padding: 25px 20px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 24px;'>Tin vui từ MenuGo</h1>
                            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Chương trình khuyến mãi đặc biệt dành cho bạn</p>
                        </div>
                        
                        <div style='padding: 30px 20px;'>
                            <h2 style='color: #0f172a; margin-top: 0; font-size: 20px;'>{savedPromotion.Name}</h2>
                            <p style='color: #475569; font-size: 15px; line-height: 1.6;'>
                                MenuGo xin thông báo chương trình khuyến mãi mới. Mời bạn tham khảo danh sách các món ăn được áp dụng ưu đãi dưới đây.
                            </p>
                            
                            <div style='background-color: #f8fafc; border-radius: 8px; padding: 15px; margin: 20px 0;'>
                                <div style='margin-bottom: 10px; display: flex;'><strong style='width: 140px; color: #475569;'>Thời gian:</strong> <span style='color: #0f172a; font-weight: 500;'>{savedPromotion.StartDate:dd/MM/yyyy HH:mm} - {savedPromotion.EndDate:dd/MM/yyyy HH:mm}</span></div>
                                <div style='display: flex;'><strong style='width: 140px; color: #475569;'>Áp dụng tại:</strong> <span style='color: #0f172a; font-weight: 500;'>{branchName}</span></div>
                            </div>

                            <h3 style='color: #0f172a; font-size: 18px; margin: 25px 0 15px 0; border-bottom: 2px solid #ea580c; display: inline-block; padding-bottom: 5px;'>Danh Sách Món Áp Dụng</h3>
                            
                            <div style='background: #fff;'>
                                {productItemsHtml}
                            </div>
                            
                            <!--
                            <div style='margin-top: 35px; text-align: center;'>
                                <p style='color: #64748b; font-size: 14px; margin-bottom: 20px;'>Nhanh chân ghé MenuGo để thưởng thức ngay nhé!</p>
                                <a href='https://menugo.vn/welcome' style='background-color: #ea580c; color: white; text-decoration: none; padding: 12px 30px; border-radius: 6px; font-weight: bold; font-size: 16px; display: inline-block;'>ĐẾN MENUGO NGAY</a>
                            </div>
                            -->
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
                        Console.WriteLine($"Error sending promotion email to {customer.Email}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in background promotion email task: {ex.Message}");
            }
        });
    }
}
