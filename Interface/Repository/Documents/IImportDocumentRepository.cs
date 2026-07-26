using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository.Documents
{
    public interface IImportDocumentRepository : IDocumentRepository
    {
        Task<Document> ExecuteImportTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);

        Task<bool> HasReturnDocumentsAsync(long importDocumentId);
        Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId);
    }
}
