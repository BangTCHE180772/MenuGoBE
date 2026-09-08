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

        return MapToDto(await _promotionRepo.GetPromotionByIdAsync(promotion.Id));
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(long id)
    {
        var promotion = await _promotionRepo.GetPromotionByIdAsync(id);
        return promotion == null ? null : MapToDto(promotion);
    }

    public async Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync(long? branchId)
    {
        var promotions = await _promotionRepo.GetAllPromotionsAsync(branchId);
        return promotions.Select(MapToDto);
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

        // Xóa quan hệ cũ trước, lưu riêng để tránh tracking conflict
        _context.PromotionProducts.RemoveRange(existing.PromotionProducts);
        _context.PromotionBranches.RemoveRange(existing.PromotionBranches);
        await _context.SaveChangesAsync();

        // Thêm quan hệ mới
        existing.PromotionProducts = dto.ProductIds
            .Select(p => new PromotionProduct { ProductId = p, PromotionId = dto.Id }).ToList();

        if (dto.Scope == PromotionScope.SpecificBranches)
        {
            existing.PromotionBranches = dto.BranchIds
                .Select(b => new PromotionBranch { BranchId = b, PromotionId = dto.Id }).ToList();
        }
        else
        {
            existing.PromotionBranches = new List<PromotionBranch>();
        }

        await _context.SaveChangesAsync();
        return MapToDto(await _promotionRepo.GetPromotionByIdAsync(dto.Id));
    }

    public async Task DeletePromotionAsync(long id)
    {
        await _promotionRepo.DeletePromotionAsync(id);
    }

    public async Task<Dictionary<long, PromotionDto>> GetActivePromotionMapForBranchAsync(long branchId)
    {
        var activePromotions = await _promotionRepo.GetActivePromotionsForBranchAsync(branchId);
        
        var map = new Dictionary<long, PromotionDto>();
        foreach (var p in activePromotions)
        {
            var dto = MapToDto(p);
            foreach (var pp in p.PromotionProducts)
            {
                map[pp.ProductId] = dto;
            }
        }
        
        return map;
    }

    private PromotionDto MapToDto(Promotion p)
    {
        var now = DateTime.UtcNow;
        string status = p.IsActive == false ? "Disabled" :
                       p.EndDate < now ? "Expired" :
                       p.StartDate > now ? "Upcoming" : "Active";

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
