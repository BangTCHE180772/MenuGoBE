using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly AppDbContext _context;

        public OrderDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDetail>> GetByOrderIdAsync(long orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderDetail?> GetByIdAsync(long id)
        {
            return await _context.OrderDetails.FindAsync(id);
        }

        public async Task CreateAsync(OrderDetail entity)
        {
            await _context.OrderDetails.AddAsync(entity);
        }

        public Task UpdateAsync(OrderDetail entity)
        {
            _context.OrderDetails.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity != null)
            {
                _context.OrderDetails.Remove(entity);
            }
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity == null) return false;

            entity.Status = status;
            if (status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
            {
                entity.CookingStatus = "Waiting";
            }
            return true;
        }

        public async Task<bool> UpdateCookingStatusAsync(long id, string cookingStatus)
        {
            var entity = await _context.OrderDetails.FindAsync(id);
            if (entity == null) return false;

            entity.CookingStatus = cookingStatus;
            return true;
        }

        public async Task<decimal> GetPendingQuantityAsync(long productId, long branchId, long? excludeOrderDetailId = null)
        {
            var directPending = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .ThenInclude(o => o.Table)
                .ThenInclude(t => t.Area)
                .Where(od => od.ProductId == productId 
                             && (!excludeOrderDetailId.HasValue || od.Id != excludeOrderDetailId.Value)
                             && od.Order.Table.Area.BranchId == branchId
                             && od.Order.Status != "Internal"
                             && (
                                 od.Status == "CustomerPending" || 
                                 (od.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed && od.Status == "Confirmed" && od.CookingStatus == "Waiting")
                             ))
                .SumAsync(od => (decimal?)od.Quantity) ?? 0m;

            var ingredientPending = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .ThenInclude(o => o.Table)
                .ThenInclude(t => t.Area)
                .Where(od => od.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed 
                             && (!excludeOrderDetailId.HasValue || od.Id != excludeOrderDetailId.Value)
                             && od.Order.Table.Area.BranchId == branchId
                             && od.Order.Status != "Internal"
                             && (od.Status == "CustomerPending" || (od.Status == "Confirmed" && od.CookingStatus == "Waiting")))
                .Join(_context.RecipesDetaileds.Where(r => r.IngredientProductId == productId),
                      od => od.ProductId,
                      r => r.ParentProductId,
                      (od, r) => new { od.Quantity, RecipeQuantity = r.Quantity })
                .SumAsync(x => (decimal?)(x.Quantity * x.RecipeQuantity)) ?? 0m;

            return directPending + ingredientPending;
        }

        public async Task<List<OrderDetail>> GetPendingByProductIdAsync(long productId, long[] branchIds)
        {
            var thirtyMinsAgo = DateTime.UtcNow.AddMinutes(-30);

            var query = _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.Table)
                .ThenInclude(t => t.Area)
                .Where(od => od.ProductId == productId &&
                             od.Order.Table != null &&
                             od.Order.Table.Area != null &&
                             branchIds.Contains(od.Order.Table.Area.BranchId) &&
                             (od.Status.ToLower() == "confirmed" || od.Status.ToLower() == "partialreturned") &&
                             (string.IsNullOrEmpty(od.CookingStatus) || od.CookingStatus.ToLower() == "waiting" || od.CookingStatus.ToLower() == "accepted") &&
                             od.CreatedAt >= thirtyMinsAgo); // Bỏ qua món chờ quá 30p
                             
            return await query.ToListAsync();
        }

        public async Task<List<OrderDetail>> GetCancelledByBranchAsync(long branchId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Table)
                        .ThenInclude(t => t.Area)
                .Where(od => od.Status == "Cancelled" &&
                             od.Order.Table != null &&
                             od.Order.Table.Area != null &&
                             od.Order.Table.Area.BranchId == branchId);

            if (fromDate.HasValue)
                query = query.Where(od => od.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(od => od.CreatedAt <= toDate.Value);

            return await query.OrderByDescending(od => od.CreatedAt).ToListAsync();
        }

        public async Task<(Dictionary<long, decimal> direct, Dictionary<long, decimal> ingredient)> GetBulkPendingQuantitiesAsync(long branchId)
        {
            var directPendingList = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .ThenInclude(o => o.Table)
                .ThenInclude(t => t.Area)
                .Where(od => od.Order.Table.Area.BranchId == branchId
                             && od.Order.Status != "Internal"
                             && (
                                 od.Status == "CustomerPending" || 
                                 (od.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed && od.Status == "Confirmed" && od.CookingStatus == "Waiting")
                             ))
                .GroupBy(od => od.ProductId)
                .Select(g => new { ProductId = g.Key, PendingQty = g.Sum(od => (decimal?)od.Quantity) ?? 0m })
                .ToListAsync();
            var directPending = directPendingList.ToDictionary(x => x.ProductId, x => x.PendingQty);

            var ingredientPendingList = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .ThenInclude(o => o.Table)
                .ThenInclude(t => t.Area)
                .Where(od => od.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed
                             && od.Order.Table.Area.BranchId == branchId
                             && od.Order.Status != "Internal"
                             && (od.Status == "CustomerPending" || (od.Status == "Confirmed" && od.CookingStatus == "Waiting")))
                .Join(_context.RecipesDetaileds,
                      od => od.ProductId,
                      r => r.ParentProductId,
                      (od, r) => new { r.IngredientProductId, od.Quantity, RecipeQuantity = r.Quantity })
                .GroupBy(x => x.IngredientProductId)
                .Select(g => new { ProductId = g.Key, PendingQty = g.Sum(x => (decimal?)(x.Quantity * x.RecipeQuantity)) ?? 0m })
                .ToListAsync();
            var ingredientPending = ingredientPendingList.ToDictionary(x => x.ProductId, x => x.PendingQty);

            return (directPending, ingredientPending);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
