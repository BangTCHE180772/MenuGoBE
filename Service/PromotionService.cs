using MenuGoBE.Data;
using MenuGoBE.Dtos.Promotion;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepo;
    private readonly AppDbContext _context;

    public PromotionService(
        IPromotionRepository promotionRepo,
        AppDbContext context)
    {
        _promotionRepo = promotionRepo;
        _context = context;
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

        return await MapToDtoAsync(await _promotionRepo.GetPromotionByIdAsync(promotion.Id));
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(long id)
    {
        var promotion = await _promotionRepo.GetPromotionByIdAsync(id);
        return promotion == null ? null : await MapToDtoAsync(promotion);
    }

    public async Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync(long? branchId)
    {
        var promotions = await _promotionRepo.GetAllPromotionsAsync(branchId);
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
        return await MapToDtoAsync(await _promotionRepo.GetPromotionByIdAsync(dto.Id));
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
}
