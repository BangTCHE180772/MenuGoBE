using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        protected readonly AppDbContext _context;

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

        public async Task<DocumentEntity> ExecuteDocumentTransactionAsync(
            DocumentEntity document,
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

        public async Task<DocumentEntity> ExecuteImportTransactionAsync(
            DocumentEntity document,
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

                // 6. Assign DocumentPartnerId to cashFlowDetails
                await _context.CashFlows.AddRangeAsync(cashFlows);
                await _context.SaveChangesAsync();

                for (int i = 0; i < cashFlows.Count; i++)
                {
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

        public async Task<bool> HasReturnDocumentsAsync(long importDocumentId)
        {
            return await _context.Documents.AnyAsync(d => d.ParentDocumentId == importDocumentId && d.Type == DocumentType.Return && !d.IsDeleted);
        }

        public async Task<DocumentEntity?> GetDocumentWithDetailsAsync(long documentId)
        {
            return await _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.Creator)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dd => dd.BInventory)
                        .ThenInclude(b => b.Product)
                .Include(d => d.DocumentPartners)
                    .ThenInclude(dp => dp.Partner)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<List<DocumentEntity>> GetDocumentsAsync(long? branchId = null, DocumentType? type = null)
        {
            var query = _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.Creator)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dd => dd.BInventory)
                        .ThenInclude(b => b.Product)
                .Include(d => d.DocumentPartners)
                    .ThenInclude(dp => dp.Partner)
                .Where(d => !d.IsDeleted);

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(d => d.BranchId == branchId.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }

            return await query
                .OrderByDescending(d => d.BusinessDate)
                .ThenByDescending(d => d.Id)
                .ToListAsync();
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
            DocumentEntity document,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedBInvs,
            List<CashFlow> cashFlowsToSoftDelete)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Documents.Update(document);

                if (ledgersToDelete.Any())
                {
                    _context.InventoryLedgers.RemoveRange(ledgersToDelete);
                }

                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }

                if (updatedBInvs.Any())
                {
                    _context.BInventories.UpdateRange(updatedBInvs);
                }

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
