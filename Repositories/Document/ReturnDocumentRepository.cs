using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Repositories.Document
{
    public class ReturnDocumentRepository : DocumentRepository, IReturnDocumentRepository
    {
        public ReturnDocumentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<DocumentEntity> ExecuteReturnTransactionAsync(
            DocumentEntity document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails)
        {
            // Sequential index: cashFlows[i] → docPartners[i]
            var indexes = Enumerable.Range(0, cashFlows.Count).ToList();
            return await ExecuteImportTransactionAsync(
                document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails, indexes);
        }
    }
}
