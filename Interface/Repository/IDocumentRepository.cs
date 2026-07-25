using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IDocumentRepository
    {
        Task<Branch?> GetBranchAsync(long branchId);
        Task<List<BInventory>> GetBInventoriesAsync(long branchId, List<long> binvIds);
        Task<List<Partner>> GetPartnersAsync(List<long> partnerIds);
        Task<List<InventoryLedger>> GetLatestLedgersBeforeOrEqualDateAsync(List<long> binvIds, DateTime date);
        Task<List<InventoryLedger>> GetLedgersAfterDateAsync(List<long> binvIds, DateTime date);
        
        Task<Document> ExecuteImportTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);
    }
}
