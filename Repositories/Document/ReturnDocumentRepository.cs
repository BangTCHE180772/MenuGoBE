using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Lấy tất cả ReturnDocumentDetail đang active (Document.IsDeleted == false && Detail.IsDeleted == false)
        /// có FatherId == importDocumentDetailId.
        /// Dùng để tính RemainingQuantity = ImportDetail.Quantity - SUM(active returns).
        /// </summary>
        public async Task<List<DocumentDetail>> GetReturnedDetailsByImportDetailIdAsync(long importDocumentDetailId)
        {
            return await _context.DocumentDetails
                .Include(d => d.Document)
                .Where(d => d.FatherId == importDocumentDetailId
                         && !d.IsDeleted
                         && d.Document != null
                         && d.Document.Type == DocumentType.Return
                         && !d.Document.IsDeleted)
                .ToListAsync();
        }

        /// <summary>
        /// Load một ImportDocumentDetail kèm BInventory (và Product), UnitConversion, Document cha.
        /// Dùng để validate và kế thừa BInventoryId, UnitConversionId cho ReturnDocumentDetail.
        /// </summary>
        public async Task<DocumentDetail?> GetImportDocumentDetailAsync(long detailId)
        {
            return await _context.DocumentDetails
                .Include(d => d.BInventory)
                    .ThenInclude(b => b.Product)
                .Include(d => d.UnitConversion)
                .Include(d => d.Document)
                    .ThenInclude(doc => doc.Branch)
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted);
        }
    }
}

