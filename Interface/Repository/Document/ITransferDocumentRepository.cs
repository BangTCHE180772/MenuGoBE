using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Repository.Document
{
    public interface ITransferDocumentRepository : IDocumentRepository
    {
        Task<DocumentEntity?> GetChildReceiveDocumentAsync(long parentTransferDocumentId);

        /// <summary>
        /// Transaction tạo đồng thời phiếu xuất chuyển (Parent) và phiếu chờ nhận (Child) tại chi nhánh nhận.
        /// </summary>
        Task<DocumentEntity> ExecuteTransferCreateTransactionAsync(
            DocumentEntity parentDoc,
            List<DocumentDetail> parentDetails,
            DocumentEntity childDoc,
            List<DocumentDetail> childDetails,
            List<BInventory> updatedFromBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate);

        /// <summary>
        /// Transaction xác nhận nhận hàng chuyển kho tại chi nhánh nhận.
        /// </summary>
        Task<DocumentEntity> ExecuteTransferReceiveTransactionAsync(
            DocumentEntity parentDoc,
            DocumentEntity childDoc,
            List<DocumentDetail> receiveDetails,
            List<BInventory> updatedToBInvs,
            List<InventoryLedger> ledgersToCreate,
            List<InventoryLedger> ledgersToUpdate);

        /// <summary>
        /// Transaction từ chối nhận hàng (hủy chuyển kho) — hoàn tác kho chi nhánh gửi.
        /// </summary>
        Task<DocumentEntity> ExecuteTransferRejectTransactionAsync(
            DocumentEntity parentDoc,
            DocumentEntity childDoc,
            List<InventoryLedger> ledgersToDelete,
            List<InventoryLedger> ledgersToUpdate,
            List<BInventory> updatedFromBInvs);
    }
}

