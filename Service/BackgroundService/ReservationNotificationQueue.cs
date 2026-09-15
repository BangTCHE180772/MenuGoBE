using System.Threading.Channels;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service;

public class ReservationNotificationQueue : IReservationNotificationQueue
{
    private readonly Channel<ReservationEmailNotification> _channel;

    public ReservationNotificationQueue()
    {
        _channel = Channel.CreateUnbounded<ReservationEmailNotification>(new UnboundedChannelOptions
        {
            SingleReader = true // Chỉ có 1 Worker đọc
        });
    }

    public async ValueTask QueueAsync(ReservationEmailNotification notification)
    {
        await _channel.Writer.WriteAsync(notification);
    }

    public async ValueTask<ReservationEmailNotification> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
