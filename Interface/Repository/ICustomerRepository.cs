using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(long id);
        Task<Customer?> GetByPhoneAsync(string phone);
        Task<Customer?> GetByEmailAsync(string email);
        Task CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task<List<Customer>> GetPromoEmailSubscribersAsync();
        Task SaveChangesAsync();
    }
}
