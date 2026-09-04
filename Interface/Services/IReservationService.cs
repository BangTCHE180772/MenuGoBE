using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Dtos.Table;

namespace MenuGoBE.Interface.Services;

public interface IReservationService
{
    Task<List<ReservationViewDto>> GetAllAsync();
    Task<List<ReservationViewDto>> GetAllFilteredAsync(List<long>? branchIds, DateTime? fromDate, DateTime? toDate);
    Task<ReservationViewDto> CreateAsync(ReservationCreateDto dto);
    Task<bool> UpdateAsync(long id, ReservationUpdateDto dto);
    Task<bool> CancelAsync(long id);
    Task<bool> RejectAsync(long id);
    Task<bool> ExtendAsync(long id, int minutes);
    Task<bool> ChangeTableAsync(long reservationId, long newTableId);
    Task<List<TableViewDto>> GetAvailableTablesAsync(long branchId, DateTime reservationTime);
    Task AssignTablesToReservationAsync(long reservationId, List<long> tableIds, bool ignoreWarning = false);
    Task<object> CheckInReservationAsync(long id);
}
