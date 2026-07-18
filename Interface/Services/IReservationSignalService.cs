namespace MenuGoBE.Interface.Services;

public interface IReservationSignalService
{
    void TriggerSignal();
    Task WaitAsync(CancellationToken cancellationToken);
}
