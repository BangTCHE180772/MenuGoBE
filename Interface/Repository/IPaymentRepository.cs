using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(long id);
    Task<Payment?> GetByTransactionIdPendingAsync(string transactionId);
    Task<Payment?> GetLatestPendingByOrderIdAsync(long orderId);
    Task<bool> HasSuccessPaymentAsync(long orderId);
    Task<Payment> CreateAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task SaveChangesAsync();
}
