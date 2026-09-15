using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByTransactionIdPendingAsync(string transactionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.Status == "Pending");
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<Payment?> GetLatestPendingByOrderIdAsync(long orderId)
    {
        return await _context.Payments
            .Where(p => p.OrderId == orderId && p.Method == "PayOS" && p.Status == "Pending")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Payment>> GetAllByOrderIdAsync(long orderId)
    {
        return await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasSuccessPaymentAsync(long orderId)
    {
        return await _context.Payments.AnyAsync(p => p.OrderId == orderId && p.Status == "Success");
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
