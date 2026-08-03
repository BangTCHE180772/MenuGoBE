namespace MenuGoBE.Interface.Services
{
    public interface IRedisInventoryService
    {
        Task<long> IncrementReservedQuantityAsync(int branchId, int productId, int quantity);
        Task<bool> TryReserveInventoryAsync(int branchId, int productId, int quantity, decimal maxQuantity);
        Task<long> DecrementReservedQuantityAsync(int branchId, int productId, int quantity);
        Task<long> GetReservedQuantityAsync(int branchId, int productId);
        Task<Dictionary<int, long>> GetReservedQuantitiesAsync(int branchId, IEnumerable<int> productIds);
        Task SetReservedQuantityAsync(int branchId, int productId, long quantity);
        Task<Dictionary<string, long>> GetAllReservedQuantitiesAsync();
        Task<Dictionary<string, long>> GetAndClearDirtyReservedQuantitiesAsync();
    }
}
