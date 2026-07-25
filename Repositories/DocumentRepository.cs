using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
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

        public async Task<List<Partner>> GetPartnersAsync(List<long> partnerIds)
        {
            return await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<List<InventoryLedger>> GetLatestLedgersBeforeOrEqualDateAsync(List<long> binvIds, DateTime date)
        {
            var ledgers = new List<InventoryLedger>();
            foreach (var binvId in binvIds)
            {
                var latest = await _context.InventoryLedgers
                    .Where(l => l.BInventoryId == binvId && l.BusinessDate <= date)
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

        public async Task<List<InventoryLedger>> GetLedgersAfterDateAsync(List<long> binvIds, DateTime date)
        {
            return await _context.InventoryLedgers
                .Where(l => binvIds.Contains(l.BInventoryId) && l.BusinessDate > date)
                .OrderBy(l => l.BusinessDate)
                .ThenBy(l => l.PostingSequence)
                .ToListAsync();
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
    }
}
