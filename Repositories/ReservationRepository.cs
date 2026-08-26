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
