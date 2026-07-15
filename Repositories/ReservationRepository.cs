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
            .Include(r => r.Order)
                .ThenInclude(o => o.Table)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
            .OrderByDescending(r => r.ReservationTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(long id)
    {
        return await _context.Reservations.FindAsync(id);
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
}
