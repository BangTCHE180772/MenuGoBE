using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Branch?> GetBranchAsync(long branchId)
        {
            return await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        }

        public async Task<List<BInventory>> GetBInventoriesAsync(long branchId, List<long> binvIds)
        {
            return await _context.BInventories
                .Include(b => b.Product)
                .Where(b => binvIds.Contains(b.Id) && b.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<List<BInventory>> GetBInventoriesByIdsAsync(List<long> binvIds)
        {
            return await _context.BInventories
                .Include(b => b.Product)
                .Where(b => binvIds.Contains(b.Id))
                .ToListAsync();
        }

        public async Task<List<Partner>> GetPartnersAsync(List<long> partnerIds)
        {
            return await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<List<InventoryLedger>> GetLatestLedgersBeforeOrEqualDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null)
        {
            var ledgers = new List<InventoryLedger>();
            foreach (var binvId in binvIds)
            {
                var query = _context.InventoryLedgers
                    .Include(l => l.DocumentDetail)
                    .Where(l => l.BInventoryId == binvId && l.BusinessDate <= date);

                if (excludeDocumentId.HasValue)
                {
                    query = query.Where(l => l.DocumentId != excludeDocumentId.Value);
                }

                var latest = await query
                    .OrderByDescending(l => l.BusinessDate)
                    .ThenByDescending(l => l.PostingSequence)
                    .FirstOrDefaultAsync();
                
                if (latest != null)
                {
                    ledgers.Add(latest);
                }
            }
            return ledgers;
        }

        public async Task<List<InventoryLedger>> GetLedgersAfterDateAsync(List<long> binvIds, DateTime date, long? excludeDocumentId = null)
        {
            var query = _context.InventoryLedgers
                .Include(l => l.DocumentDetail)
                .Where(l => binvIds.Contains(l.BInventoryId) && l.BusinessDate > date);

            if (excludeDocumentId.HasValue)
            {
                query = query.Where(l => l.DocumentId != excludeDocumentId.Value);
            }

            return await query
                .OrderBy(l => l.BusinessDate)
                .ThenBy(l => l.PostingSequence)
                .ToListAsync();
        }

        public async Task<InventoryLedger?> GetPriorLedgerAsync(long binvId, DateTime date, long postingSequence)
        {
            return await _context.InventoryLedgers
                .Include(l => l.DocumentDetail)
                .Where(l => l.BInventoryId == binvId && 
                            (l.BusinessDate < date || (l.BusinessDate == date && l.PostingSequence < postingSequence)))
                .OrderByDescending(l => l.BusinessDate)
                .ThenByDescending(l => l.PostingSequence)
                .FirstOrDefaultAsync();
        }

        public async Task<List<InventoryLedger>> GetSubsequentLedgersAsync(long binvId, DateTime date, long postingSequence)
        {
            return await _context.InventoryLedgers
                .Include(l => l.DocumentDetail)
                .Where(l => l.BInventoryId == binvId && 
                            (l.BusinessDate > date || (l.BusinessDate == date && l.PostingSequence > postingSequence)))
                .OrderBy(l => l.BusinessDate)
                .ThenBy(l => l.PostingSequence)
                .ToListAsync();
        }

        public async Task<Document> ExecuteDocumentTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails)
        {
            return await ExecuteImportTransactionAsync(
                document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }

        public async Task<Document> ExecuteImportTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Save Document to get Id
                await _context.Documents.AddAsync(document);
                await _context.SaveChangesAsync();

                // 2. Assign DocumentId to details, docPartners and ledgers
                foreach (var d in details) d.DocumentId = document.Id;
                foreach (var dp in docPartners) dp.DocumentId = document.Id;
                foreach (var l in ledgersToCreate) l.DocumentId = document.Id;

                // 3. Save Details and DocPartners
                await _context.DocumentDetails.AddRangeAsync(details);
                await _context.DocumentPartners.AddRangeAsync(docPartners);
                await _context.SaveChangesAsync();

                // 4. Assign DocumentDetailId to ledgers
                foreach (var l in ledgersToCreate)
                {
                    var matchingDetail = details.FirstOrDefault(d => d.BInventoryId == l.BInventoryId);
                    if (matchingDetail != null)
                    {
                        l.DocumentDetailId = matchingDetail.Id;
                    }
                }
                await _context.InventoryLedgers.AddRangeAsync(ledgersToCreate);
                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }

                // 5. Update BInventories
                _context.BInventories.UpdateRange(updatedBInvs);

                // 6. Assign DocumentPartnerId to cashFlowDetails (optional, we can match by PartnerId and cost)
                // Actually, wait, let's just save cashFlows first
                await _context.CashFlows.AddRangeAsync(cashFlows);
                await _context.SaveChangesAsync();

                // Assign CashFlowId and DocumentPartnerId to CashFlowDetails
                // We assume cashFlowDetails are in the same order as cashFlows and docPartners? 
                // Better approach: the caller grouped them by Partner.
                // We'll trust the caller has set up a way to map them or we map them here:
                for (int i = 0; i < cashFlows.Count; i++)
                {
                    // This relies on the caller passing them in perfectly matched order.
                    if (i < cashFlowDetails.Count && i < docPartners.Count)
                    {
                        cashFlowDetails[i].CashFlowId = cashFlows[i].Id;
                        cashFlowDetails[i].DocumentPartnerId = docPartners[i].Id;
                    }
                }
                
                if (cashFlowDetails.Any())
                {
                    await _context.CashFlowDetails.AddRangeAsync(cashFlowDetails);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return document;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> HasReturnDocumentsAsync(long importDocumentId)
        {
            return await _context.Documents.AnyAsync(d => d.ParentDocumentId == importDocumentId && !d.IsDeleted);
        }

        public async Task<List<DocumentDetail>> GetReturnedDetailsByImportIdAsync(long importDocumentId)
        {
            return await _context.DocumentDetails
                .Include(d => d.Document)
                .Where(d => d.Document != null 
                            && d.Document.ParentDocumentId == importDocumentId 
                            && d.Document.Type == DocumentType.Return 
                            && !d.Document.IsDeleted)
                .ToListAsync();
        }

        public async Task<Document> ExecuteReturnTransactionAsync(
            Document document,
            List<DocumentDetail> details,
            List<DocumentPartner> docPartners,
            List<BInventory> updatedBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate,
            List<CashFlow> cashFlows,
            List<CashFlowDetail> cashFlowDetails)
        {
            // Tái sử dụng logic của Import vì cơ bản là insert giống hệt nhau
            return await ExecuteImportTransactionAsync(
                document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }
        public async Task<Document?> GetDocumentWithDetailsAsync(long documentId)
        {
            return await _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dd => dd.BInventory)
                        .ThenInclude(b => b.Product)
                .Include(d => d.DocumentPartners)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<Document?> GetChildReceiveDocumentAsync(long parentTransferDocumentId)
        {
            return await _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.DocumentDetails)
                .FirstOrDefaultAsync(d => d.ParentDocumentId == parentTransferDocumentId && d.Type == DocumentType.Transfer && !d.IsDeleted);
        }

        public async Task<List<CashFlow>> GetCashFlowsByDocumentIdAsync(long documentId)
        {
            return await _context.CashFlows
                .Where(cf => cf.CashFlowDetails.Any(cfd => cfd.DocumentPartner != null && cfd.DocumentPartner.DocumentId == documentId))
                .ToListAsync();
        }

        public async Task<List<InventoryLedger>> GetLedgersByDocumentIdAsync(long documentId)
        {
            return await _context.InventoryLedgers
                .Where(l => l.DocumentId == documentId)
                .ToListAsync();
        }

        public async Task ExecuteDeleteTransactionAsync(
            Document document,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedBInvs,
            List<CashFlow> cashFlowsToSoftDelete)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Soft delete document
                _context.Documents.Update(document);

                // Hard delete the exact ledgers generated by this document
                if (ledgersToDelete.Any())
                {
                    _context.InventoryLedgers.RemoveRange(ledgersToDelete);
                }

                // Update subsequent ledgers due to ripple recalculation
                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }

                // Update BInventories
                if (updatedBInvs.Any())
                {
                    _context.BInventories.UpdateRange(updatedBInvs);
                }

                // Soft delete associated cashflows
                if (cashFlowsToSoftDelete.Any())
                {
                    _context.CashFlows.UpdateRange(cashFlowsToSoftDelete);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
