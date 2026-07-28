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

        // ──────────────────────────────────────────────────────────────────────
        // A. TẠI CHI NHÁNH GỬI (XUẤT CHUYỂN - TRANSFER DOCUMENT)
        // ──────────────────────────────────────────────────────────────────────
        public async Task<DocumentEntity> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null)
        {
            var fromBranch = await _repo.GetBranchAsync(dto.FromBranchId);
            var toBranch = await _repo.GetBranchAsync(dto.ToBranchId);

            if (fromBranch == null || toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound, "Không tìm thấy chi nhánh chuyển hoặc chi nhánh nhận.");

            if (fromBranch.Id == toBranch.Id)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound, "Chi nhánh nhận phải khác chi nhánh gửi.");

            if (fromBranch.PostingLockDate.HasValue && dto.BusinessDate.Date <= fromBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày chứng từ nằm trước hoặc bằng ngày khóa sổ của chi nhánh xuất.");

            var fromBinvIds = dto.Details.Select(d => d.FromBInventoryId).Distinct().ToList();
            var fromBinvs = await _repo.GetBInventoriesByIdsAsync(fromBinvIds);

            if (fromBinvs.Count != fromBinvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Một hoặc nhiều sản phẩm chuyển kho không tồn tại tại chi nhánh gửi.");

            // RÀNG BUỘC HÀNG HÓA: KHÔNG CHO PHÉP hàng hóa loại PROCESSED
            foreach (var binv in fromBinvs)
            {
                if (binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể chuyển kho sản phẩm [{binv.Product.Name}] do thuộc loại Processed.");
            }

            // Stock Audit Lock Validation – Chi nhánh gửi (ERR_STOCK_AUDIT_LOCKED)
            await ValidateStockAuditLockAsync(fromBinvIds, dto.BusinessDate);

            // Load UnitConversions nếu có
            var ucIds = dto.Details
                .Where(d => d.UnitConversionId.HasValue)
                .Select(d => d.UnitConversionId!.Value)
                .Distinct().ToList();

            var unitConversions = await _repo.GetUnitConversionsByIdsAsync(ucIds);

            var now = DateTime.UtcNow;
            string parentCode = $"TR{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            string childCode = $"RC{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var parentDocument = new DocumentEntity
            {
                BranchId = dto.FromBranchId,
                BusinessDate = exactSecondDate,
                Code = parentCode,
                Type = DocumentType.Transfer,
                TransferStatus = DocumentTransferStatus.Pending, // Chờ nhận
                TotalAmount = 0,
                CreatedBy = createdBy ?? dto.CreatedByAccountId,
                CreatedAt = now
            };

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(fromBinvIds, parentDocument.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(fromBinvIds, parentDocument.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            // ── Kiểm tra tồn kho tại mốc BusinessDate ────────────────────────
            var runningState = new Dictionary<long, (decimal qty, decimal totalValue, decimal avgCost, decimal leftOver)>();
            foreach (var binvId in fromBinvIds)
            {
                var prior = beforeLedgers.FirstOrDefault(l => l.BInventoryId == binvId);
                decimal pq  = prior?.RunningQuantity    ?? 0;
                decimal pav = prior?.RunningAverageCost ?? 0;
                decimal plo = prior?.RunningLeftOver     ?? 0;
                runningState[binvId] = (pq, (pq * pav) + plo, pav, plo);
            }

            // Gom tổng số lượng quy đổi cần xuất theo từng BInventoryId
            var requiredQtyByBinv = dto.Details
                .GroupBy(d => d.FromBInventoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d =>
                    {
                        decimal cp = 1m;
                        if (d.UnitConversionId.HasValue)
                        {
                            var uc = unitConversions.FirstOrDefault(u => u.Id == d.UnitConversionId.Value);
                            if (uc != null) cp = uc.ConversionPoint;
                        }
                        return d.Quantity * cp;
                    })
                );

            foreach (var (binvId, requiredQty) in requiredQtyByBinv)
            {
                decimal availableQty = runningState[binvId].qty;
                if (requiredQty > availableQty)
                {
                    var binvName = fromBinvs.FirstOrDefault(b => b.Id == binvId)?.Product?.Name ?? $"BInventoryId={binvId}";
                    throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded,
                        $"Không đủ tồn kho sản phẩm [{binvName}] tại chi nhánh gửi ở mốc {exactSecondDate:dd/MM/yyyy}. " +
                        $"Tồn hiện tại: {availableQty:0.###}, Cần chuyển: {requiredQty:0.###}.");
                }
            }

            // ── Build DocumentDetails & InventoryLedgers cho Chi nhánh Gửi ────
            var parentDetails = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedFromBInvs = new List<BInventory>();
            decimal totalTransferValue = 0;

            foreach (var detail in dto.Details)
            {
                var fromBinv = fromBinvs.First(b => b.Id == detail.FromBInventoryId);

                UnitConversion? uc = detail.UnitConversionId.HasValue
                    ? unitConversions.FirstOrDefault(u => u.Id == detail.UnitConversionId.Value)
                    : null;

                decimal conversionPoint = uc?.ConversionPoint ?? 1m;
                decimal systemQuantity = detail.Quantity * conversionPoint;

                var (runQ, runTV, runAvg, runLO) = runningState[fromBinv.Id];

                // Nếu user nhập UnitPrice thì phải là số nguyên >= 1
                if (detail.UnitPrice.HasValue)
                {
                    if (detail.UnitPrice.Value < 1m || detail.UnitPrice.Value % 1m != 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice,
                            $"Đơn giá chuyển kho ({detail.UnitPrice.Value}) phải là số nguyên lớn hơn hoặc bằng 1.");
                    }
                }

                decimal unitCost = detail.UnitPrice.HasValue
                    ? (conversionPoint > 0 ? detail.UnitPrice.Value / conversionPoint : detail.UnitPrice.Value)
                    : runAvg;

                var fromDocDetail = new DocumentDetail
                {
                    BInventoryId = fromBinv.Id,
                    UnitConversionId = detail.UnitConversionId,
                    Quantity = detail.Quantity,
                    SystemQuantity = systemQuantity,
                    UnitPrice = detail.UnitPrice ?? runAvg,
                    Note = $"Xuất chuyển sang kho {toBranch.Name}: {detail.Note}",
                    CreatedAt = now
                };
                parentDetails.Add(fromDocDetail);

                var fromLedger = new InventoryLedger
                {
                    BInventoryId = fromBinv.Id,
                    BusinessDate = parentDocument.BusinessDate,
                    DocumentType = parentDocument.Type,
                    QuantityDelta = -systemQuantity, // Outflow (âm)
                    InventoryValueDelta = 0,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = unitCost,
                    CreatedAt = now
                };

                RecalculateLedgerState(fromLedger, runQ, runTV, runAvg, runLO);
                decimal transferredValue = Math.Abs(fromLedger.InventoryValueDelta);
                totalTransferValue += transferredValue;

                runningState[fromBinv.Id] = (
                    fromLedger.RunningQuantity,
                    fromLedger.RunningInventoryValue,
                    fromLedger.RunningAverageCost,
                    fromLedger.RunningLeftOver
                );

                ledgersToCreate.Add(fromLedger);
            }

            // RÀNG BUỘC: Tổng giá trị chuyển kho không vượt quá 10 tỷ đồng
            if (totalTransferValue > 10_000_000_000m)
            {
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit,
                    $"Tổng giá trị chuyến chuyển kho ({totalTransferValue:N0} VNĐ) vượt quá giới hạn tối đa 10 tỷ đồng.");
            }

            // Ripple-recalculate subsequent ledgers tại chi nhánh gửi

            foreach (var binvId in fromBinvIds)
            {
                var (runQ, runTV, runAvg, runLO) = runningState[binvId];

                if (afterLedgersDict.TryGetValue(binvId, out var subsequentFromLedgers))
                {
                    foreach (var ledger in subsequentFromLedgers)
                    {
                        RecalculateLedgerState(ledger, runQ, runTV, runAvg, runLO);
                        runQ = ledger.RunningQuantity;
                        runTV = ledger.RunningInventoryValue;
                        runAvg = ledger.RunningAverageCost;
                        runLO = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                var fromBinv = fromBinvs.First(b => b.Id == binvId);
                fromBinv.Quantity = runQ;
                fromBinv.Avg = runAvg;
                fromBinv.LeftOver = runLO;
                updatedFromBInvs.Add(fromBinv);
            }

            parentDocument.TotalAmount = totalTransferValue;

            // ── TỰ ĐỘNG TẠO PHIẾU NHẬN TẠI CHI NHÁNH NHẬN (Child Document - Type ReceiveTransfer) ────
            var childDocument = new DocumentEntity
            {
                BranchId = dto.ToBranchId,
                BusinessDate = exactSecondDate,
                Code = childCode,
                Type = DocumentType.ReceiveTransfer, // Type 7: Nhận chuyển kho
                TransferStatus = DocumentTransferStatus.Pending, // Chờ chi nhánh nhận bấm nhận
                TotalAmount = totalTransferValue,
                CreatedBy = createdBy ?? dto.CreatedByAccountId,
                CreatedAt = now
            };


            var childDetails = new List<DocumentDetail>();
            foreach (var detail in dto.Details)
            {
                var childDocDetail = new DocumentDetail
                {
                    BInventoryId = detail.ToBInventoryId,
                    UnitConversionId = detail.UnitConversionId,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity * (unitConversions.FirstOrDefault(u => u.Id == detail.UnitConversionId)?.ConversionPoint ?? 1m),
                    UnitPrice = detail.UnitPrice ?? 0,
                    Note = $"Chờ nhận chuyển kho từ {fromBranch.Name}: {detail.Note}",
                    CreatedAt = now
                };
                childDetails.Add(childDocDetail);
            }

            return await _repo.ExecuteTransferCreateTransactionAsync(
                parentDocument, parentDetails,
                childDocument, childDetails,
                updatedFromBInvs, ledgersToCreate, ledgersToUpdate);
        }

        // ──────────────────────────────────────────────────────────────────────
        // B. THAO TÁC XỬ LÝ TẠI CHI NHÁNH NHẬN (NHẬN HÀNG)
        // ──────────────────────────────────────────────────────────────────────
        public async Task<DocumentEntity> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null)
        {
            var parentTransferDoc = await _repo.GetDocumentWithDetailsAsync(dto.TransferDocumentId);
            if (parentTransferDoc == null || parentTransferDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy phiếu chuyển kho gốc hoặc phiếu đã bị xóa.");

            if (parentTransferDoc.Type != DocumentType.Transfer)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Chứng từ tham chiếu không phải phiếu chuyển kho.");

            if (parentTransferDoc.TransferStatus == DocumentTransferStatus.Received)
                throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, "Phiếu chuyển kho này đã được nhận trước đó.");

            if (parentTransferDoc.TransferStatus == DocumentTransferStatus.Rejected)
                throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, "Phiếu chuyển kho này đã bị từ chối/hủy.");

            var childReceiveDoc = await _repo.GetChildReceiveDocumentAsync(parentTransferDoc.Id);

            var rawReceiveDate = dto.ReceivedDate.ToUniversalTime();
            var exactReceiveDate = new DateTime(rawReceiveDate.Year, rawReceiveDate.Month, rawReceiveDate.Day, rawReceiveDate.Hour, rawReceiveDate.Minute, rawReceiveDate.Second, DateTimeKind.Utc);

            // RÀNG BUỘC NGÀY NHẬN 1: Ngày nhận BẮT BUỘC phải > Ngày gửi hàng (SendBusinessDate)
            if (exactReceiveDate.Date <= parentTransferDoc.BusinessDate.Date)
                throw new MenuGoException(ErrorCodes.DocumentTransferReceiveDateMustBeAfterSendDate,
                    $"Ngày nhận hàng ({exactReceiveDate:dd/MM/yyyy}) BẮT BUỘC phải lớn hơn Ngày gửi hàng ({parentTransferDoc.BusinessDate:dd/MM/yyyy}).");

            var toBinvIds = dto.Details.Select(d => d.ToBInventoryId).Distinct().ToList();
            var toBinvs = await _repo.GetBInventoriesByIdsAsync(toBinvIds);

            if (toBinvs.Count != toBinvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Một hoặc nhiều sản phẩm nhận kho không tồn tại.");

            var toBranchId = toBinvs.First().BranchId;
            var toBranch = await _repo.GetBranchAsync(toBranchId);

            if (toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            // RÀNG BUỘC NGÀY NHẬN 2: > Ngày khóa sổ của chi nhánh nhận
            if (toBranch.PostingLockDate.HasValue && exactReceiveDate.Date <= toBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày nhận hàng nằm trước hoặc bằng ngày khóa sổ của chi nhánh nhận.");

            // Stock Audit Lock Validation – Chi nhánh nhận (ERR_STOCK_AUDIT_LOCKED)
            await ValidateStockAuditLockAsync(toBinvIds, exactReceiveDate);

            // Load UnitConversions
            var ucIds = dto.Details
                .Where(d => d.ToBInventoryId > 0)
                .Select(d => d.ToBInventoryId)
                .Distinct().ToList();

            // Cập nhật trạng thái phiếu
            parentTransferDoc.TransferStatus = DocumentTransferStatus.Received;

            if (childReceiveDoc == null)
            {
                var now = DateTime.UtcNow;
                childReceiveDoc = new DocumentEntity
                {
                    BranchId = toBranchId,
                    BusinessDate = exactReceiveDate,
                    Code = $"RC{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}",
                    Type = DocumentType.ReceiveTransfer,
                    TransferStatus = DocumentTransferStatus.Received,
                    TotalAmount = parentTransferDoc.TotalAmount,
                    ParentDocumentId = parentTransferDoc.Id,
                    CreatedBy = createdBy ?? dto.CreatedByAccountId,
                    CreatedAt = now
                };
            }
            else
            {
                childReceiveDoc.BusinessDate = exactReceiveDate;
                childReceiveDoc.Type = DocumentType.ReceiveTransfer;
                childReceiveDoc.TransferStatus = DocumentTransferStatus.Received;
            }

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(toBinvIds, exactReceiveDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(toBinvIds, exactReceiveDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var receiveDetails = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedToBInvs = new List<BInventory>();

            foreach (var detail in dto.Details)
            {
                var toBinv = toBinvs.First(b => b.Id == detail.ToBInventoryId);
                var parentDetail = parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.BInventory?.ProductId == toBinv.ProductId)
                    ?? parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.Id == childReceiveDoc?.DocumentDetails.FirstOrDefault(cd => cd.BInventoryId == toBinv.Id)?.FatherId);

                // GIÁ TRÊN TỪNG UNIT HÀNG BẮT BUỘC KẾ THỪA CHÍNH XÁC TỪ BÊN XUẤT
                decimal unitPrice = parentDetail?.UnitPrice ?? 0;
                long? unitConversionId = parentDetail?.UnitConversionId;

                decimal conversionPoint = 1m;
                if (unitConversionId.HasValue)
                {
                    var ucList = await _repo.GetUnitConversionsByIdsAsync(new List<long> { unitConversionId.Value });
                    if (ucList.Any()) conversionPoint = ucList.First().ConversionPoint;
                }

                decimal systemQuantity = detail.ReceivedQuantity * conversionPoint;
                decimal transferredValue = detail.ReceivedQuantity * unitPrice;
                decimal unitCost = conversionPoint > 0 ? unitPrice / conversionPoint : unitPrice;

                var docDetail = childReceiveDoc.DocumentDetails.FirstOrDefault(d => d.BInventoryId == toBinv.Id)
                    ?? new DocumentDetail
                    {
                        BInventoryId = toBinv.Id,
                        FatherId = parentDetail?.Id,
                        UnitConversionId = unitConversionId,
                        Quantity = detail.ReceivedQuantity,
                        SystemQuantity = systemQuantity,
                        UnitPrice = unitPrice,
                        Note = detail.Note ?? $"Nhận chuyển kho từ phiếu {parentTransferDoc.Code}",
                        CreatedAt = DateTime.UtcNow
                    };

                docDetail.Quantity = detail.ReceivedQuantity;
                docDetail.SystemQuantity = systemQuantity;
                docDetail.UnitPrice = unitPrice;
                docDetail.UnitConversionId = unitConversionId;
                receiveDetails.Add(docDetail);

                var priorToLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == toBinv.Id);
                decimal toQty = priorToLedger?.RunningQuantity ?? 0;
                decimal toAvg = priorToLedger?.RunningAverageCost ?? 0;
                decimal toLeftOver = priorToLedger?.RunningLeftOver ?? 0;
                decimal toVal = (toQty * toAvg) + toLeftOver;

                var toLedger = new InventoryLedger
                {
                    BInventoryId = toBinv.Id,
                    BusinessDate = exactReceiveDate,
                    DocumentType = DocumentType.ReceiveTransfer,
                    QuantityDelta = systemQuantity, // Inflow (+) theo base unit
                    InventoryValueDelta = transferredValue,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = unitCost,
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
                updatedToBInvs.Add(toBinv);
            }

            return await _repo.ExecuteTransferReceiveTransactionAsync(
                parentTransferDoc, childReceiveDoc, receiveDetails,
                updatedToBInvs, ledgersToCreate, ledgersToUpdate);
        }

        // ──────────────────────────────────────────────────────────────────────
        // B2. THAO TÁC XỬ LÝ TẠI CHI NHÁNH NHẬN (TỪ CHỐI NHẬN - REJECT)
        // ──────────────────────────────────────────────────────────────────────
        public async Task<DocumentEntity> RejectTransferDocumentAsync(TransferRejectDocumentDto dto, long? rejectedBy = null)
        {
            var parentTransferDoc = await _repo.GetDocumentWithDetailsAsync(dto.TransferDocumentId);
            if (parentTransferDoc == null || parentTransferDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy phiếu chuyển kho gốc.");

            if (parentTransferDoc.TransferStatus == DocumentTransferStatus.Received)
                throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, "Phiếu chuyển kho này đã được nhận, không thể từ chối.");

            var childReceiveDoc = await _repo.GetChildReceiveDocumentAsync(parentTransferDoc.Id);

            // Cập nhật trạng thái Rejected
            parentTransferDoc.TransferStatus = DocumentTransferStatus.Rejected;
            if (childReceiveDoc != null)
            {
                childReceiveDoc.TransferStatus = DocumentTransferStatus.Rejected;
                childReceiveDoc.IsDeleted = true;
            }

            // Hàng hóa NGAY LẬP TỨC quay trở lại kho Chi nhánh Gửi:
            // Xóa các InventoryLedger xuất chuyển ở chi nhánh gửi và ripple-recalculate
            var fromBinvIds = parentTransferDoc.DocumentDetails.Select(d => d.BInventoryId).Distinct().ToList();
            var fromBinvs = await _repo.GetBInventoriesByIdsAsync(fromBinvIds);
            var ledgersToDelete = await _repo.GetLedgersByDocumentIdAsync(parentTransferDoc.Id);

            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedFromBInvs = new List<BInventory>();

            foreach (var binv in fromBinvs)
            {
                var deletedLedger = ledgersToDelete.FirstOrDefault(l => l.BInventoryId == binv.Id);
                if (deletedLedger == null) continue;

                var priorLedger = await _repo.GetPriorLedgerAsync(binv.Id, deletedLedger.BusinessDate, deletedLedger.PostingSequence);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;
                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                var subsequentLedgers = await _repo.GetSubsequentLedgersAsync(binv.Id, deletedLedger.BusinessDate, deletedLedger.PostingSequence);

                foreach (var ledger in subsequentLedgers)
                {
                    RecalculateLedgerState(ledger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);
                    runningQuantity = ledger.RunningQuantity;
                    currentTotalValue = ledger.RunningInventoryValue;
                    runningAvg = ledger.RunningAverageCost;
                    runningLeftOver = ledger.RunningLeftOver;
                    ledgersToUpdate.Add(ledger);
                }

                binv.Quantity = runningQuantity;
                binv.Avg = runningAvg;
                binv.LeftOver = runningLeftOver;
                updatedFromBInvs.Add(binv);
            }

            return await _repo.ExecuteTransferRejectTransactionAsync(
                parentTransferDoc, childReceiveDoc ?? parentTransferDoc,
                ledgersToDelete, ledgersToUpdate, updatedFromBInvs);
        }

        // ──────────────────────────────────────────────────────────────────────
        // C. RÀNG BUỘC XÓA PHIẾU
        // ──────────────────────────────────────────────────────────────────────
        public async Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null)
        {
            var doc = await _repo.GetDocumentWithDetailsAsync(documentId);
            if (doc == null || doc.IsDeleted)
                return;

            // Xác định phiếu parent gốc (nếu xóa ở phía chi nhánh nhận)
            long parentId = doc.ParentDocumentId ?? doc.Id;
            var parentDoc = doc.ParentDocumentId.HasValue
                ? await _repo.GetDocumentWithDetailsAsync(doc.ParentDocumentId.Value)
                : doc;

            if (parentDoc != null)
            {
                var fromBranch = await _repo.GetBranchAsync(parentDoc.BranchId);
                if (fromBranch != null && fromBranch.PostingLockDate.HasValue
                    && parentDoc.BusinessDate.Date <= fromBranch.PostingLockDate.Value.Date)
                {
                    throw new MenuGoException(ErrorCodes.DocumentTransferFromBranchPostingLockError,
                        $"Không thể xóa phiếu do chi nhánh gửi [{fromBranch.Name}] đã khóa sổ tại ngày gửi hàng ({parentDoc.BusinessDate:dd/MM/yyyy}).");
                }

                // Stock Audit Lock Validation – chi nhánh gửi khi xóa (ERR_STOCK_AUDIT_LOCKED)
                var parentFromBinvIds = parentDoc.DocumentDetails.Select(d => d.BInventoryId).Distinct().ToList();
                await ValidateStockAuditLockAsync(parentFromBinvIds, parentDoc.BusinessDate);
            }

            // Nếu xóa phiếu chuyển gốc, xóa cả phiếu chờ nhận con
            if (doc.ParentDocumentId == null)
            {
                var childReceiveDoc = await _repo.GetChildReceiveDocumentAsync(doc.Id);
                if (childReceiveDoc != null && !childReceiveDoc.IsDeleted)
                {
                    await DeleteDocumentAsync(childReceiveDoc.Id, deletedBy);
                }
            }

            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}

