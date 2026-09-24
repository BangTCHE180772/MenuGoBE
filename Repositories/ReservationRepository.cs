using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Reservation>> GetAllWithDetailsAsync()
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Branch)
            .Include(r => r.Order)
                .ThenInclude(o => o.Table)
                    .ThenInclude(t => t.Area)
                        .ThenInclude(a => a.Branch)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
            .OrderByDescending(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetFilteredWithDetailsAsync(List<long>? branchIds, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Reservations.AsNoTracking();

        // 1. Filter by date OR pending status
        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(r => (r.ReservationTime >= fromDate.Value && r.ReservationTime <= toDate.Value) || r.Status == "Pending");
        }

        // 2. Filter by branches if provided
        if (branchIds != null && branchIds.Any())
        {
            // The reservation itself belongs to the branch OR its table belongs to the branch
            query = query.Where(r => 
                (r.BranchId.HasValue && branchIds.Contains(r.BranchId.Value)) || 
                (r.Order != null && r.Order.Table != null && r.Order.Table.Area != null && branchIds.Contains(r.Order.Table.Area.BranchId))
            );
        }

        return await query
            .Include(r => r.Customer)
            .Include(r => r.Branch)
            .Include(r => r.Order)
                .ThenInclude(o => o.Table)
                    .ThenInclude(t => t.Area)
                        .ThenInclude(a => a.Branch)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
            .OrderByDescending(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(long id)
    {
        return await _context.Reservations
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task CreateAsync(Reservation entity)
    {
        await _context.Reservations.AddAsync(entity);
    }

    public Task UpdateAsync(Reservation entity)
    {
        _context.Reservations.Update(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Reservation>> GetOverlappingReservationsAsync(DateTime startTime, DateTime endTime)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Order)
            .Where(r => r.Status == "Pending" || r.Status == "Confirmed")
            .Where(r => r.ReservationTime >= startTime && r.ReservationTime <= endTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByOrderIdAsync(long orderId)
    {
        return await _context.Reservations
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    public async Task<Reservation?> GetUpcomingByTableIdAsync(long tableId)
    {
        var now = DateTime.UtcNow;
        var lateLimit = now.AddMinutes(-60); // Allow late check-in
        return await _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Order)
            .Where(r => (r.Status == "Pending" || r.Status == "Confirmed") && r.ReservationTime >= lateLimit && r.OrderId.HasValue)
            .Where(r => r.Order!.TableId == tableId || _context.Orders.Any(child => child.FatherId == r.OrderId && child.TableId == tableId))
            .OrderBy(r => r.ReservationTime) // get the earliest reservation first

            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsTableProtectedAsync(long tableId, int thresholdMinutes = 60)
    {
        var now = DateTime.UtcNow;
        var thresholdTime = now.AddMinutes(thresholdMinutes);

        var hasUpcoming = await _context.Reservations
            .Where(r => r.Status == "Pending" || r.Status == "Confirmed")
            .Where(r => r.ReservationTime >= now && r.ReservationTime <= thresholdTime)
            .Where(r => r.OrderId.HasValue)
            .AnyAsync(r => 
                r.Order!.TableId == tableId || 
                _context.Orders.Any(child => child.FatherId == r.OrderId && child.TableId == tableId)
            );

        return hasUpcoming;
    }
}
