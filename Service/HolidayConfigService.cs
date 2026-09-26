using MenuGoBE.Data;
using MenuGoBE.Dtos.HolidayConfig;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service;

public class HolidayConfigService : IHolidayConfigService
{
    private readonly AppDbContext _context;

    public HolidayConfigService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HolidayConfigViewDto>> GetAllAsync(long? branchId = null)
    {
        var allBranches = await _context.Branches.ToDictionaryAsync(b => b.Id, b => b.Name);

        var items = await _context.HolidayConfigs
            .Include(h => h.Branch)
            .Include(h => h.Creator)
            .OrderByDescending(h => h.FromDate)
            .ToListAsync();

        if (branchId.HasValue && branchId.Value > 0)
        {
            items = items.Where(h =>
            {
                var bIds = ParseBranchIds(h.BranchIds, h.BranchId);
                return !bIds.Any() || bIds.Contains(branchId.Value);
            }).ToList();
        }

        return items.Select(h =>
        {
            var bIds = ParseBranchIds(h.BranchIds, h.BranchId);
            var bNames = bIds.Select(id => allBranches.TryGetValue(id, out var name) ? name : $"Chi nhánh #{id}").ToList();

            return new HolidayConfigViewDto
            {
                Id = h.Id,
                BranchId = h.BranchId,
                BranchName = h.Branch?.Name,
                BranchIds = bIds,
                BranchNames = bNames,
                Name = h.Name,
                FromDate = h.FromDate,
                ToDate = h.ToDate,
                Coefficient = h.Coefficient,
                IsRecurring = h.IsRecurring,
                IsActive = h.IsActive,
                CreatedBy = h.CreatedBy,
                CreatorName = h.Creator?.Name,
                CreatedAt = h.CreatedAt
            };
        }).ToList();
    }

    public async Task<HolidayConfigViewDto?> GetByIdAsync(long id)
    {
        var h = await _context.HolidayConfigs
            .Include(x => x.Branch)
            .Include(x => x.Creator)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (h == null) return null;

        var allBranches = await _context.Branches.ToDictionaryAsync(b => b.Id, b => b.Name);
        var bIds = ParseBranchIds(h.BranchIds, h.BranchId);
        var bNames = bIds.Select(id => allBranches.TryGetValue(id, out var name) ? name : $"Chi nhánh #{id}").ToList();

        return new HolidayConfigViewDto
        {
            Id = h.Id,
            BranchId = h.BranchId,
            BranchName = h.Branch?.Name,
            BranchIds = bIds,
            BranchNames = bNames,
            Name = h.Name,
            FromDate = h.FromDate,
            ToDate = h.ToDate,
            Coefficient = h.Coefficient,
            IsRecurring = h.IsRecurring,
            IsActive = h.IsActive,
            CreatedBy = h.CreatedBy,
            CreatorName = h.Creator?.Name,
            CreatedAt = h.CreatedAt
        };
    }

    public async Task<HolidayConfigViewDto> CreateAsync(HolidayConfigCreateDto dto)
    {
        if (dto.FromDate > dto.ToDate)
        {
            throw new InvalidOperationException("Từ ngày không thể lớn hơn Đến ngày.");
        }

        var bIds = dto.BranchIds != null && dto.BranchIds.Any()
            ? dto.BranchIds.Where(id => id > 0).Distinct().ToList()
            : (dto.BranchId > 0 ? new List<long> { dto.BranchId.Value } : new List<long>());

        var entity = new HolidayConfig
        {
            BranchId = bIds.Count == 1 ? bIds.First() : null,
            BranchIds = bIds.Any() ? string.Join(",", bIds) : null,
            Name = dto.Name,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            Coefficient = dto.Coefficient,
            IsRecurring = dto.IsRecurring,
            IsActive = dto.IsActive,
            CreatedBy = dto.CreatedBy.HasValue && dto.CreatedBy.Value > 0 ? dto.CreatedBy.Value : null,
            CreatedAt = DateTime.UtcNow
        };

        await _context.HolidayConfigs.AddAsync(entity);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<bool> UpdateAsync(HolidayConfigUpdateDto dto)
    {
        var entity = await _context.HolidayConfigs.FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (entity == null) return false;

        if (dto.FromDate > dto.ToDate)
        {
            throw new InvalidOperationException("Từ ngày không thể lớn hơn Đến ngày.");
        }

        var bIds = dto.BranchIds != null && dto.BranchIds.Any()
            ? dto.BranchIds.Where(id => id > 0).Distinct().ToList()
            : (dto.BranchId > 0 ? new List<long> { dto.BranchId.Value } : new List<long>());

        entity.BranchId = bIds.Count == 1 ? bIds.First() : null;
        entity.BranchIds = bIds.Any() ? string.Join(",", bIds) : null;
        entity.Name = dto.Name;
        entity.FromDate = dto.FromDate;
        entity.ToDate = dto.ToDate;
        entity.Coefficient = dto.Coefficient;
        entity.IsRecurring = dto.IsRecurring;
        entity.IsActive = dto.IsActive;

        _context.HolidayConfigs.Update(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _context.HolidayConfigs.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return false;

        _context.HolidayConfigs.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private static List<long> ParseBranchIds(string? branchIdsStr, long? singleBranchId)
    {
        if (!string.IsNullOrWhiteSpace(branchIdsStr))
        {
            return branchIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s.Trim(), out var val) ? val : 0)
                .Where(v => v > 0)
                .Distinct()
                .ToList();
        }
        if (singleBranchId.HasValue && singleBranchId.Value > 0)
        {
            return new List<long> { singleBranchId.Value };
        }
        return new List<long>();
    }
}
