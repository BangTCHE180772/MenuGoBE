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

        Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId);
    }
}
