using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllWithDetailsAsync();
    Task<Reservation?> GetByIdAsync(long id);
    Task CreateAsync(Reservation entity);
    Task UpdateAsync(Reservation entity);
    Task SaveChangesAsync();
    Task<List<Reservation>> GetOverlappingReservationsAsync(DateTime startTime, DateTime endTime);
}
