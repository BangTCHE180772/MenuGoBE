using MenuGoBE.Dtos.Partner;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IPartnerRepository
    {
        Task<(List<Partner> Partners, int TotalCount)> GetPagedPartnersAsync(PartnerQueryDto query);
        Task<Partner?> GetPartnerByIdAsync(long id);
        Task<Partner> CreatePartnerAsync(Partner partner);
        Task<Partner> UpdatePartnerAsync(Partner partner);
        Task<bool> DeletePartnerAsync(Partner partner);
        Task<bool> CheckPartnerInTransactionsAsync(long partnerId);
    }
}
