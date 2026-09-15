namespace MenuGoBE.Interface.Services;

public interface IReservationNotificationQueue
{
    ValueTask QueueAsync(MenuGoBE.Dtos.Reservation.ReservationEmailNotification notification);
    ValueTask<MenuGoBE.Dtos.Reservation.ReservationEmailNotification> DequeueAsync(CancellationToken cancellationToken);
}
