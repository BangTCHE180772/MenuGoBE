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
    public class TransferDocumentRepository : DocumentRepository, ITransferDocumentRepository
    {
        public TransferDocumentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<DocumentEntity?> GetChildReceiveDocumentAsync(long parentTransferDocumentId)
        {
            return await _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.DocumentDetails)
                .FirstOrDefaultAsync(d => d.ParentDocumentId == parentTransferDocumentId
                                          && (d.Type == DocumentType.ReceiveTransfer || d.Type == DocumentType.Transfer)
                                          && !d.IsDeleted);
        }


        public async Task<DocumentEntity> ExecuteTransferCreateTransactionAsync(
            DocumentEntity parentDoc,
            List<DocumentDetail> parentDetails,
            DocumentEntity childDoc,
            List<DocumentDetail> childDetails,
            List<BInventory> updatedFromBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu phiếu parent (xuất chuyển ở chi nhánh gửi)
                await _context.Documents.AddAsync(parentDoc);
                await _context.SaveChangesAsync();

                // Assign ParentDocumentId và DocumentId
                foreach (var pd in parentDetails) pd.DocumentId = parentDoc.Id;
                foreach (var l in ledgersToCreate) l.DocumentId = parentDoc.Id;

                await _context.DocumentDetails.AddRangeAsync(parentDetails);
                await _context.SaveChangesAsync();

                for (int i = 0; i < ledgersToCreate.Count; i++)
                {
                    if (i < parentDetails.Count)
                    {
                        ledgersToCreate[i].DocumentDetailId = parentDetails[i].Id;
                    }
                }
                await _context.InventoryLedgers.AddRangeAsync(ledgersToCreate);
                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }
                _context.BInventories.UpdateRange(updatedFromBInvs);

                // 2. Lưu phiếu child (chờ nhận ở chi nhánh nhận)
                childDoc.ParentDocumentId = parentDoc.Id;
                await _context.Documents.AddAsync(childDoc);
                await _context.SaveChangesAsync();

                for (int i = 0; i < childDetails.Count; i++)
                {
                    childDetails[i].DocumentId = childDoc.Id;
                    if (i < parentDetails.Count)
                    {
                        childDetails[i].FatherId = parentDetails[i].Id; // Liên kết dòng chi tiết
                    }
                }
                await _context.DocumentDetails.AddRangeAsync(childDetails);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return parentDoc;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DocumentEntity> ExecuteTransferReceiveTransactionAsync(
            DocumentEntity parentDoc,
            DocumentEntity childDoc,
            List<DocumentDetail> receiveDetails,
            List<BInventory> updatedToBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cập nhật trạng thái phiếu parent & child
                _context.Documents.Update(parentDoc);
                _context.Documents.Update(childDoc);

                // 2. Lưu chi tiết phiếu nhận (nếu có chi tiết mới/cập nhật)
                foreach (var rd in receiveDetails)
                {
                    if (rd.Id > 0)
                        _context.DocumentDetails.Update(rd);
                    else
                    {
                        rd.DocumentId = childDoc.Id;
                        await _context.DocumentDetails.AddAsync(rd);
                    }
                }
                await _context.SaveChangesAsync();

                // 3. Gán DocumentDetailId cho các ledger tăng tồn kho tại chi nhánh nhận
                for (int i = 0; i < ledgersToCreate.Count; i++)
                {
                    ledgersToCreate[i].DocumentId = childDoc.Id;
                    if (i < receiveDetails.Count)
                    {
                        ledgersToCreate[i].DocumentDetailId = receiveDetails[i].Id;
                    }
                }

                await _context.InventoryLedgers.AddRangeAsync(ledgersToCreate);
                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }
                _context.BInventories.UpdateRange(updatedToBInvs);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return childDoc;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DocumentEntity> ExecuteTransferRejectTransactionAsync(
            DocumentEntity parentDoc,
            DocumentEntity childDoc,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedFromBInvs)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái parent & child thành Rejected
                _context.Documents.Update(parentDoc);
                _context.Documents.Update(childDoc);

                // Xóa/Hoàn tác các ledger xuất chuyển ở chi nhánh gửi
                if (ledgersToDelete.Any())
                {
                    _context.InventoryLedgers.RemoveRange(ledgersToDelete);
                }
                if (ledgersToUpdate.Any())
                {
                    _context.InventoryLedgers.UpdateRange(ledgersToUpdate);
                }
                if (updatedFromBInvs.Any())
                {
                    _context.BInventories.UpdateRange(updatedFromBInvs);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return childDoc;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

