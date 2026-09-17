using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class LeftoverRecordRepository : ILeftoverRecordRepository
    {
        private readonly AppDbContext _context;

        public LeftoverRecordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeftoverRecord>> GetByBranchAsync(long branchId, LeftoverType? type, DateOnly? fromDate, DateOnly? toDate)
        {
            var query = _context.LeftoverRecords
                .Include(l => l.Product)
                .Include(l => l.Shift)
                .Include(l => l.Creator)
                .Include(l => l.AtFaultAccount)
                .Include(l => l.OrderDetail)
                    .ThenInclude(od => od!.Order)
                        .ThenInclude(o => o.Table)
                .Where(l => l.BranchId == branchId);

            if (type.HasValue)
                query = query.Where(l => l.Type == type.Value);

            if (fromDate.HasValue)
                query = query.Where(l => l.RecordDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.RecordDate <= toDate.Value);

            return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<LeftoverRecord?> GetByIdAsync(long id)
        {
            return await _context.LeftoverRecords
                .Include(l => l.Product)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<LeftoverRecord>> GetReusableAsync(long branchId, DateTime sinceUtc, long? productId)
        {
            var query = _context.LeftoverRecords
                .Include(l => l.Product)
                .Include(l => l.OrderDetail)
                    .ThenInclude(od => od!.Order)
                        .ThenInclude(o => o.Table)
                .Where(l => l.BranchId == branchId
                         && l.Type == LeftoverType.Return
                         && l.UsedAt == null
                         && l.CreatedAt >= sinceUtc);

            if (productId.HasValue)
                query = query.Where(l => l.ProductId == productId.Value);

            return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<OrderDetail?> GetOrderDetailWithTableAsync(long orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                    .ThenInclude(o => o.Table)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);
        }

        public async Task CreateAsync(LeftoverRecord entity)
        {
            await _context.LeftoverRecords.AddAsync(entity);
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.LeftoverRecords.FindAsync(id);
            if (entity != null)
            {
                _context.LeftoverRecords.Remove(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
