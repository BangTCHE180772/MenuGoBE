using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface IReturnDocumentRepository : IDocumentRepository
    {
        Task<DocumentEntity> ExecuteReturnTransactionAsync(
            DocumentEntity document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);

        /// <summary>
        /// Lấy tất cả ReturnDocumentDetail đang active (IsDeleted == false)
        /// có FatherId == importDocumentDetailId, dùng để tính RemainingQuantity per dòng nhập gốc.
        /// </summary>
        Task<List<DocumentDetail>> GetReturnedDetailsByImportDetailIdAsync(long importDocumentDetailId);

        /// <summary>
        /// Load một ImportDocumentDetail cụ thể kèm BInventory, Product, UnitConversion và Document cha.
        /// </summary>
        Task<DocumentDetail?> GetImportDocumentDetailAsync(long detailId);
    }
}

