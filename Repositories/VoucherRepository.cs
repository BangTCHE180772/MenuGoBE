using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories;

public class VoucherRepository : IVoucherRepository
{
    private readonly AppDbContext _context;

    public VoucherRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Voucher>> GetAllAsync()
    {
        return await _context.Vouchers.Include(v => v.Branch).ToListAsync();
    }

    public async Task<Voucher?> GetByIdAsync(long id)
    {
        return await _context.Vouchers.Include(v => v.Branch).FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Voucher?> GetByCodeAsync(string code)
    {
        return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code.ToUpper() == code.ToUpper());
    }

    public async Task<Voucher> CreateAsync(Voucher voucher)
    {
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return voucher;
    }

    public async Task<bool> UpdateAsync(Voucher voucher)
    {
        _context.Vouchers.Update(voucher);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return false;

        _context.Vouchers.Remove(voucher);
        return await _context.SaveChangesAsync() > 0;
    }
}
