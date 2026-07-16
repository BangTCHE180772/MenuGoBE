using MenuGoBE.Dtos.Reservation;

namespace MenuGoBE.Interface.Services;

public interface IReservationService
{
    Task<List<ReservationViewDto>> GetAllAsync();
    Task<ReservationViewDto> CreateAsync(ReservationCreateDto dto);
    Task<bool> CheckInAsync(long id);
    Task<bool> CancelAsync(long id);
    Task<bool> ChangeTableAsync(long reservationId, long newTableId);
}
