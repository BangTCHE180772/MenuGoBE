using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;

namespace MenuGoBE.Interface.Repository
{
    public interface IDocumentRepository
    {
        Task<Branch?> GetBranchAsync(long branchId);
        Task<List<BInventory>> GetBInventoriesAsync(long branchId, List<long> binvIds);
        Task<List<BInventory>> GetBInventoriesByIdsAsync(List<long> binvIds);
        Task<List<Partner>> GetPartnersAsync(List<long> partnerIds);
        Task<List<InventoryLedger>> GetLatestLedgersBeforeOrEqualDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null);
        Task<List<InventoryLedger>> GetLedgersAfterDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null);
        Task<InventoryLedger?> GetPriorLedgerAsync(long binvId, DateTime date, long postingSequence);
        Task<List<InventoryLedger>> GetSubsequentLedgersAsync(long binvId, DateTime date, long postingSequence);
        
        Task<Document> ExecuteDocumentTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);

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
        
        Task<Document> ExecuteReturnTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);
            
        Task<Document?> GetDocumentWithDetailsAsync(long documentId);
        Task<Document?> GetChildReceiveDocumentAsync(long parentTransferDocumentId);
        Task<List<CashFlow>> GetCashFlowsByDocumentIdAsync(long documentId);
        Task<List<InventoryLedger>> GetLedgersByDocumentIdAsync(long documentId);
        Task ExecuteDeleteTransactionAsync(
            Document document,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedBInvs,
            List<CashFlow> cashFlowsToSoftDelete);
    }
}
