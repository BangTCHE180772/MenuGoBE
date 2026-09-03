using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllWithDetailsAsync();
    Task<List<Reservation>> GetFilteredWithDetailsAsync(List<long>? branchIds, DateTime? fromDate, DateTime? toDate);
    Task<Reservation?> GetByIdAsync(long id);
    Task CreateAsync(Reservation entity);
    Task UpdateAsync(Reservation entity);
    Task SaveChangesAsync();
    Task<List<Reservation>> GetOverlappingReservationsAsync(DateTime startTime, DateTime endTime);
    Task<Reservation?> GetByOrderIdAsync(long orderId);
    Task<Reservation?> GetUpcomingByTableIdAsync(long tableId);
    Task<bool> IsTableProtectedAsync(long tableId, int thresholdMinutes = 60);
}
