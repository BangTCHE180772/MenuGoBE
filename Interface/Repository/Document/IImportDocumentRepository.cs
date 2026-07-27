using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface IImportDocumentRepository : IDocumentRepository
    {
        /// <summary>
        /// Thực hiện transaction tạo phiếu nhập kho.
        /// cashFlowPartnerIndexes[i] = index trong docPartners tương ứng với cashFlows[i],
        /// để liên kết DocumentPartnerId chính xác khi nhiều dòng cùng BInventoryId.
        /// </summary>
        Task<DocumentEntity> ExecuteImportTransactionAsync(
            DocumentEntity document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails,
            List<int> cashFlowPartnerIndexes);

        new Task<bool> HasReturnDocumentsAsync(long importDocumentId);
        Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId);
    }
}

