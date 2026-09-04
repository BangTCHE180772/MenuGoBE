using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Partner;

namespace MenuGoBE.Interface.Services
{
    public interface IPartnerService
    {
        Task<PagedResult<PartnerViewDto>> GetPagedPartnersAsync(PartnerQueryDto query);
        Task<PartnerViewDto> GetPartnerByIdAsync(long id, int? mode = null);
        Task<List<PartnerViewDto>> GetPartnersByBranchAsync(long branchId, string? search = null, int? mode = null);
        Task<PartnerViewDto> CreatePartnerAsync(PartnerCreateDto dto, long? createdBy = null);
        Task<PartnerViewDto> UpdatePartnerAsync(long id, PartnerUpdateDto dto);
        Task<bool> DeletePartnerAsync(long id);

        Task<PartnerFinancialSummaryDto> GetPartnerFinancialSummaryAsync(long partnerId);
        Task<List<PartnerImportDocumentDto>> GetPartnerImportDocumentsAsync(long partnerId);
        Task<List<PartnerTransactionHistoryDto>> GetPartnerCashFlowHistoryAsync(long partnerId);
        Task<List<CustomerPurchaseHistoryDto>> GetCustomerPurchaseHistoryAsync(long customerId);
        Task<List<CustomerPointHistoryDto>> GetCustomerPointHistoryAsync(long customerId);
    }
}
