using MenuGoBE.Interface.Services;
using System.Threading;
using System.Threading.Tasks;

namespace MenuGoBE.Service;

public class ReservationSignalService : IReservationSignalService
{
    // A SemaphoreSlim is a perfect lightweight wait-handle that supports async await
    // We start with 0 count. WaitAsync will block until TriggerSignal is called (which increments the count).
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

    public void TriggerSignal()
    {
        // If the semaphore is currently 0, we increment it by 1 so the waiter wakes up.
        // If it's already > 0 (multiple signals fired before task woke up), we just let it be.
        if (_semaphore.CurrentCount == 0)
        {
            try
            {
                _semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignored - safely handles race conditions
            }
        }
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
    }
}
