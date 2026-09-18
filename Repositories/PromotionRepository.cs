using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _context;

    public PromotionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
    {
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();
        return promotion;
    }

    public async Task<Promotion?> GetPromotionByIdAsync(long id)
    {
        return await _context.Promotions
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
                    .ThenInclude(prod => prod.Image)
            .Include(p => p.PromotionBranches)
                .ThenInclude(pb => pb.Branch)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync()
    {
        return await _context.Promotions
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .Include(p => p.PromotionBranches)
                .ThenInclude(pb => pb.Branch)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Promotion>> GetAllPromotionsByBranchAsync(long branchId)
    {
        return await _context.Promotions
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .Include(p => p.PromotionBranches)
                .ThenInclude(pb => pb.Branch)
            .Where(p => 
                p.Scope == PromotionScope.AllBranches || 
                p.PromotionBranches.Any(pb => pb.BranchId == branchId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Promotion> UpdatePromotionAsync(Promotion promotion)
    {
        _context.Promotions.Update(promotion);
        await _context.SaveChangesAsync();
        return promotion;
    }

    public async Task DeletePromotionAsync(long id)
    {
        var promotion = await _context.Promotions
            .Include(p => p.PromotionProducts)
            .Include(p => p.PromotionBranches)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (promotion != null)
        {
            // Xóa bản ghi con trước (phòng trường hợp cascade chưa migration)
            _context.PromotionProducts.RemoveRange(promotion.PromotionProducts);
            _context.PromotionBranches.RemoveRange(promotion.PromotionBranches);
            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasOverlappingPromotionAsync(long productId, long? branchId, DateTime startDate, DateTime endDate, long? excludePromotionId = null)
    {
        var query = _context.PromotionProducts
            .Include(pp => pp.Promotion)
                .ThenInclude(p => p.PromotionBranches)
            .Where(pp => pp.ProductId == productId)
            .Where(pp => pp.Promotion.IsActive)
            .Where(pp => pp.Promotion.StartDate <= endDate && pp.Promotion.EndDate >= startDate);

        if (excludePromotionId.HasValue)
        {
            query = query.Where(pp => pp.PromotionId != excludePromotionId.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(pp => 
                pp.Promotion.Scope == PromotionScope.AllBranches || 
                pp.Promotion.PromotionBranches.Any(pb => pb.BranchId == branchId.Value));
        }

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Promotion>> GetActivePromotionsForBranchAsync(long branchId)
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .Include(p => p.PromotionProducts)
            .Where(p => p.IsActive)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .Where(p => p.Scope == PromotionScope.AllBranches || p.PromotionBranches.Any(pb => pb.BranchId == branchId))
            .ToListAsync();
    }
}
