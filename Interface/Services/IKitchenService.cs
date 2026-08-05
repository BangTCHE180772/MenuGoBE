using MenuGoBE.Dtos.Kitchen;

namespace MenuGoBE.Interface.Services;

public interface IKitchenService
{
    Task<bool> RequestRestockAsync(RequestRestockDto dto, long? accountId);
    Task<bool> HandleInternalRestockCompletionAsync(long orderDetailId);
}
