using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;

using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Service.Document
{
    public class TransferDocumentService : DocumentServiceBase, ITransferDocumentService
    {
        private new readonly ITransferDocumentRepository _repo;

        public TransferDocumentService(ITransferDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DocumentEntity> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null)
        {
            var fromBranch = await _repo.GetBranchAsync(dto.FromBranchId);
            var toBranch = await _repo.GetBranchAsync(dto.ToBranchId);

            if (fromBranch == null || toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound, "Không tìm thấy chi nhánh chuyển hoặc nhận.");

            if (fromBranch.PostingLockDate.HasValue && dto.BusinessDate.Date <= fromBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày chứng từ nằm trước ngày khóa sổ của chi nhánh xuất.");

            var fromBinvIds = dto.Details.Select(d => d.FromBInventoryId).Distinct().ToList();
            var fromBinvs = await _repo.GetBInventoriesByIdsAsync(fromBinvIds);

            foreach (var binv in fromBinvs)
            {
                if (binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể chuyển kho sản phẩm [{binv.Product.Name}] do thuộc loại Processed.");
            }

            var now = DateTime.UtcNow;
            string code = $"TR{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = dto.FromBranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.Pending, // Treo hàng chờ kho nhận bấm nhận
                TotalAmount = 0,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(fromBinvIds, document.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(fromBinvIds, document.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();
            decimal totalTransferValue = 0;

            foreach (var detail in dto.Details)
            {
                var fromBinv = fromBinvs.FirstOrDefault(b => b.Id == detail.FromBInventoryId);
                if (fromBinv == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Sản phẩm chuyển kho không tồn tại tại kho xuất.");

                var priorFromLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == fromBinv.Id);
                decimal fromQty = priorFromLedger?.RunningQuantity ?? 0;
                decimal fromAvg = priorFromLedger?.RunningAverageCost ?? 0;
                decimal fromLeftOver = priorFromLedger?.RunningLeftOver ?? 0;

                // RÀNG BUỘC: Chi nhánh xuất phải có đủ tồn kho tại mốc thời gian chuyển
                if (fromQty < detail.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Không thể chuyển kho sản phẩm [{fromBinv.Product.Name}]. Số lượng chuyển ({detail.Quantity}) lớn hơn số tồn kho tại chi nhánh xuất tại mốc thời gian ({fromQty}).");
                }

                decimal fromVal = (fromQty * fromAvg) + fromLeftOver;

                var fromDocDetail = new DocumentDetail
                {
                    BInventoryId = fromBinv.Id,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = fromAvg,
                    Note = $"Xuất chuyển sang kho {toBranch.Name}: {detail.Note}",
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(fromDocDetail);

                var fromLedger = new InventoryLedger
                {
                    BInventoryId = fromBinv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = -detail.Quantity,
                    InventoryValueDelta = 0,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = fromAvg,
                    DocumentDetail = fromDocDetail,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(fromLedger, fromQty, fromVal, fromAvg, fromLeftOver);
                decimal transferredValue = Math.Abs(fromLedger.InventoryValueDelta);
                totalTransferValue += transferredValue;

                fromQty = fromLedger.RunningQuantity;
                fromVal = fromLedger.RunningInventoryValue;
                fromAvg = fromLedger.RunningAverageCost;
                fromLeftOver = fromLedger.RunningLeftOver;
                ledgersToCreate.Add(fromLedger);

                if (afterLedgersDict.TryGetValue(fromBinv.Id, out var subsequentFromLedgers))
                {
                    foreach (var ledger in subsequentFromLedgers)
                    {
                        RecalculateLedgerState(ledger, fromQty, fromVal, fromAvg, fromLeftOver);
                        fromQty = ledger.RunningQuantity;
                        fromVal = ledger.RunningInventoryValue;
                        fromAvg = ledger.RunningAverageCost;
                        fromLeftOver = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                fromBinv.Quantity = fromQty;
                fromBinv.Avg = fromAvg;
                fromBinv.LeftOver = fromLeftOver;
                updatedBInvs.Add(fromBinv);
            }

            document.TotalAmount = totalTransferValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        public async Task<DocumentEntity> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null)
        {
            var parentTransferDoc = await _repo.GetDocumentWithDetailsAsync(dto.TransferDocumentId);
            if (parentTransferDoc == null || parentTransferDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy phiếu chuyển kho gốc hoặc phiếu đã bị xóa.");

            if (parentTransferDoc.Type != DocumentType.Transfer)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Chứng từ tham chiếu không phải phiếu chuyển kho.");

            if (parentTransferDoc.TransferStatus == DocumentTransferStatus.Received)
                throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, "Phiếu chuyển kho này đã được nhận trước đó.");

            var rawReceiveDate = dto.ReceivedDate.ToUniversalTime();
            var exactReceiveDate = new DateTime(rawReceiveDate.Year, rawReceiveDate.Month, rawReceiveDate.Day, rawReceiveDate.Hour, rawReceiveDate.Minute, rawReceiveDate.Second, DateTimeKind.Utc);

            if (exactReceiveDate.Date < parentTransferDoc.BusinessDate.Date)
                throw new MenuGoException(ErrorCodes.DocumentInvalidReturnBusinessDate, "Ngày nhận hàng không được nhỏ hơn ngày xuất chuyển từ kho gửi.");

            var toBinvIds = dto.Details.Select(d => d.ToBInventoryId).Distinct().ToList();
            var toBinvs = await _repo.GetBInventoriesByIdsAsync(toBinvIds);

            if (toBinvs.Count != toBinvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Một hoặc nhiều sản phẩm nhận kho không tồn tại.");

            var toBranchId = toBinvs.First().BranchId;
            var toBranch = await _repo.GetBranchAsync(toBranchId);

            if (toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (toBranch.PostingLockDate.HasValue && exactReceiveDate.Date <= toBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày nhận hàng nằm trước hoặc bằng ngày khóa sổ của chi nhánh nhận.");

            // Kiểm tra số lượng nhận không vượt quá số lượng gửi
            foreach (var detail in dto.Details)
            {
                var toBinv = toBinvs.First(b => b.Id == detail.ToBInventoryId);
                if (toBinv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể nhận kho sản phẩm [{toBinv.Product.Name}] do thuộc loại Processed.");

                var parentDetail = parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.BInventory.ProductId == toBinv.ProductId);
                if (parentDetail == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Sản phẩm [{toBinv.Product.Name}] không nằm trong danh sách hàng gửi.");

                if (detail.ReceivedQuantity > parentDetail.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Số lượng thực nhận ({detail.ReceivedQuantity}) không được lớn hơn số lượng xuất gửi từ chi nhánh gốc ({parentDetail.Quantity}).");
                }
            }

            var now = DateTime.UtcNow;
            string code = $"RC{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var receiveDocument = new DocumentEntity
            {
                BranchId = toBranchId,
                BusinessDate = exactReceiveDate,
                Code = code,
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.Received,
                TotalAmount = parentTransferDoc.TotalAmount,
                ParentDocumentId = parentTransferDoc.Id, // Father ID
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            // Cập nhật phiếu gốc sang Received
            parentTransferDoc.TransferStatus = DocumentTransferStatus.Received;

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(toBinvIds, receiveDocument.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(toBinvIds, receiveDocument.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();

            foreach (var detail in dto.Details)
            {
                var toBinv = toBinvs.First(b => b.Id == detail.ToBInventoryId);
                var parentDetail = parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.BInventory.ProductId == toBinv.ProductId);
                
                decimal transferredValue = parentDetail != null ? parentDetail.Quantity * parentDetail.UnitPrice : 0;
                decimal unitCost = detail.ReceivedQuantity > 0 ? transferredValue / detail.ReceivedQuantity : 0;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = toBinv.Id,
                    Quantity = detail.ReceivedQuantity,
                    SystemQuantity = detail.ReceivedQuantity,
                    UnitPrice = unitCost,
                    Note = detail.Note ?? $"Nhận chuyển kho từ phiếu {parentTransferDoc.Code}",
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var priorToLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == toBinv.Id);
                decimal toQty = priorToLedger?.RunningQuantity ?? 0;
                decimal toAvg = priorToLedger?.RunningAverageCost ?? 0;
                decimal toLeftOver = priorToLedger?.RunningLeftOver ?? 0;
                decimal toVal = (toQty * toAvg) + toLeftOver;

                var toLedger = new InventoryLedger
                {
                    BInventoryId = toBinv.Id,
                    BusinessDate = receiveDocument.BusinessDate,
                    DocumentType = receiveDocument.Type,
                    QuantityDelta = detail.ReceivedQuantity,
                    InventoryValueDelta = transferredValue,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = unitCost,
                    DocumentDetail = docDetail,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(toLedger, toQty, toVal, toAvg, toLeftOver);
                toQty = toLedger.RunningQuantity;
                toVal = toLedger.RunningInventoryValue;
                toAvg = toLedger.RunningAverageCost;
                toLeftOver = toLedger.RunningLeftOver;
                ledgersToCreate.Add(toLedger);

                if (afterLedgersDict.TryGetValue(toBinv.Id, out var subsequentToLedgers))
                {
                    foreach (var ledger in subsequentToLedgers)
                    {
                        RecalculateLedgerState(ledger, toQty, toVal, toAvg, toLeftOver);
                        toQty = ledger.RunningQuantity;
                        toVal = ledger.RunningInventoryValue;
                        toAvg = ledger.RunningAverageCost;
                        toLeftOver = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                toBinv.Quantity = toQty;
                toBinv.Avg = toAvg;
                toBinv.LeftOver = toLeftOver;
                updatedBInvs.Add(toBinv);
            }

            return await _repo.ExecuteDocumentTransactionAsync(
                receiveDocument, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        public async Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null)
        {
            var doc = await _repo.GetDocumentWithDetailsAsync(documentId);
            if (doc == null || doc.IsDeleted)
                return;

            if (doc.Branch.PostingLockDate.HasValue && doc.BusinessDate.Date <= doc.Branch.PostingLockDate.Value.Date)
            {
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, $"Không thể xóa phiếu chuyển kho do ngày chứng từ nằm trước hoặc bằng ngày khóa sổ của chi nhánh {doc.Branch.Name}.");
            }

            // Nếu đây là phiếu chuyển kho gốc (Parent)
            if (doc.ParentDocumentId == null)
            {
                var childReceiveDoc = await _repo.GetChildReceiveDocumentAsync(doc.Id);
                if (childReceiveDoc != null && !childReceiveDoc.IsDeleted)
                {
                    if (childReceiveDoc.Branch.PostingLockDate.HasValue && childReceiveDoc.BusinessDate.Date <= childReceiveDoc.Branch.PostingLockDate.Value.Date)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, $"Không thể xóa do chi nhánh nhận {childReceiveDoc.Branch.Name} đã khóa sổ.");
                    }
                    await DeleteDocumentAsync(childReceiveDoc.Id, deletedBy);
                }
            }

            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
