using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Repository
{
    public interface IDocumentRepository
    {
        Task<Branch?> GetBranchAsync(long branchId);
        Task<List<BInventory>> GetBInventoriesAsync(long branchId, List<long> binvIds);
        Task<List<BInventory>> GetBInventoriesByIdsAsync(List<long> binvIds);
        Task<List<BInventory>> GetBInventoriesByProductIdsAsync(long branchId, List<long> productIds);
        Task<List<Partner>> GetPartnersAsync(List<long> partnerIds);
        Task<List<InventoryLedger>> GetLatestLedgersBeforeOrEqualDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null);
        Task<List<InventoryLedger>> GetLedgersAfterDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null);
        Task<InventoryLedger?> GetPriorLedgerAsync(long binvId, DateTime date, long postingSequence);
        Task<List<InventoryLedger>> GetSubsequentLedgersAsync(long binvId, DateTime date, long postingSequence);
        
        Task<DocumentEntity> ExecuteDocumentTransactionAsync(
            DocumentEntity document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails);
            
        Task<DocumentEntity?> GetDocumentWithDetailsAsync(long documentId);
        Task<List<DocumentEntity>> GetDocumentsAsync(long? branchId = null, MenuGoBE.Models.Enums.DocumentType? type = null);
        Task<bool> HasReturnDocumentsAsync(long importDocumentId);
        Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId);
        Task<List<DocumentEntity>> GetReturnDocumentsByImportIdAsync(long importDocumentId);
        Task<List<CashFlow>> GetCashFlowsByDocumentIdAsync(long documentId);
        Task<List<InventoryLedger>> GetLedgersByDocumentIdAsync(long documentId);
        Task ExecuteDeleteTransactionAsync(
            DocumentEntity document,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedBInvs,
            List<CashFlow> cashFlowsToSoftDelete);

        /// <summary>
        /// Lấy danh sách UnitConversion theo danh sách Id.
        /// </summary>
        Task<List<UnitConversion>> GetUnitConversionsByIdsAsync(List<long> ids);

        /// <summary>
        /// Trả về InventoryLedger loại Check (Phiếu kiểm kho) mới nhất (theo BusinessDate)
        /// cho từng BInventoryId trong danh sách. Dùng để kiểm tra Business Date Lock.
        /// Include navigation Document để lấy Code cho error message.
        /// </summary>
        Task<List<InventoryLedger>> GetLatestCheckLedgersForBInventoriesAsync(List<long> binvIds);
    }
}
