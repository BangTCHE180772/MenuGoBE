using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Services
{
    public interface ICustomerPortalService
    {
        Task<CustomerProfileDto> GetProfileAsync(long customerId);
        Task<CustomerProfileDto> UpdateProfileAsync(long customerId, CustomerUpdateProfileDto dto);
        Task<List<ReservationViewDto>> GetMyReservationsAsync(long customerId);
        Task<object> CreateReservationAsync(long customerId, ReservationCreateDto dto);
        Task<bool> CancelReservationAsync(long customerId, long reservationId);
        Task<List<Notification>> GetMyNotificationsAsync(long customerId);
    }
}
