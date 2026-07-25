using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Partner;

namespace MenuGoBE.Interface.Services
{
    public interface IPartnerService
    {
        Task<PagedResult<PartnerViewDto>> GetPagedPartnersAsync(PartnerQueryDto query);
        Task<PartnerViewDto> GetPartnerByIdAsync(long id);
        Task<PartnerViewDto> CreatePartnerAsync(PartnerCreateDto dto, long? createdBy = null);
        Task<PartnerViewDto> UpdatePartnerAsync(long id, PartnerUpdateDto dto);
        Task<bool> DeletePartnerAsync(long id);
    }
}
