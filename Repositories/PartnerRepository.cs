using MenuGoBE.Data;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class PartnerRepository : IPartnerRepository
    {
        private readonly AppDbContext _context;

        public PartnerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Partner> Partners, int TotalCount)> GetPagedPartnersAsync(PartnerQueryDto query)
        {
            var queryable = _context.Partners
                .Include(p => p.Address)
                .Include(p => p.Creator)
                .AsQueryable();

            if (query.BranchId.HasValue)
            {
                queryable = queryable.Where(p => p.BranchId == query.BranchId.Value);
            }

            if (query.Mode.HasValue)
            {
                queryable = query.Mode.Value switch
                {
                    // Mode 1: Supplier
                    1 => queryable.Where(p => p.Type == PartnerType.Supplier),
                    // Mode 2: Transport and Other
                    2 => queryable.Where(p => p.Type == PartnerType.Transporter || p.Type == PartnerType.Other),
                    // Mode 3: Customer
                    3 => queryable.Where(p => p.Type == PartnerType.Customer),
                    _ => queryable
                };
            }
            else if (query.Type.HasValue)
            {
                queryable = queryable.Where(p => p.Type == query.Type.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(p => 
                    p.Name.ToLower().Contains(search) || 
                    (p.Phone != null && p.Phone.Contains(search)) ||
                    (p.Email != null && p.Email.ToLower().Contains(search)));
            }

            // Sorting
            queryable = query.SortBy?.ToLower() switch
            {
                "name" => query.IsDescending ? queryable.OrderByDescending(p => p.Name) : queryable.OrderBy(p => p.Name),
                "createdat" => query.IsDescending ? queryable.OrderByDescending(p => p.CreatedAt) : queryable.OrderBy(p => p.CreatedAt),
                _ => queryable.OrderByDescending(p => p.CreatedAt)
            };

            int totalCount = await queryable.CountAsync();
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Partner?> GetPartnerByIdAsync(long id)
        {
            return await _context.Partners
                .Include(p => p.Address)
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Partner> CreatePartnerAsync(Partner partner)
        {
            await _context.Partners.AddAsync(partner);
            await _context.SaveChangesAsync();
            return partner;
        }

        public async Task<Partner> UpdatePartnerAsync(Partner partner)
        {
            _context.Partners.Update(partner);
            await _context.SaveChangesAsync();
            return partner;
        }

        public async Task<bool> DeletePartnerAsync(Partner partner)
        {
            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckPartnerInTransactionsAsync(long partnerId)
        {
            // Check if partner exists in Documents
            var inDocuments = await _context.Documents.AnyAsync(d => d.PartnerId == partnerId);
            if (inDocuments) return true;

            // Check if partner exists in CashFlows
            var inCashFlows = await _context.CashFlows.AnyAsync(cf => cf.PartnerId == partnerId);
            if (inCashFlows) return true;

            return false;
        }
    }
}
