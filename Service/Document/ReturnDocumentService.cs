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
    public class ReturnDocumentService : DocumentServiceBase, IReturnDocumentService
    {
        private new readonly IReturnDocumentRepository _repo;

        public ReturnDocumentService(IReturnDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        // ──────────────────────────────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────────────────────────────
        public async Task<DocumentEntity> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null)
        {
            // ── 1. Validate phiếu nhập gốc ────────────────────────────────────
            var importDoc = await _repo.GetDocumentWithDetailsAsync(dto.ImportDocumentId);
            if (importDoc == null || importDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound,
                    "Không tìm thấy phiếu nhập gốc hoặc phiếu đã bị xóa.");

            if (importDoc.Type != DocumentType.Import)
                throw new MenuGoException(ErrorCodes.DocumentNotFound,
                    "Chứng từ tham chiếu không phải là phiếu nhập kho.");

            if (dto.BusinessDate.Date < importDoc.BusinessDate.Date)
                throw new MenuGoException(ErrorCodes.DocumentInvalidReturnBusinessDate,
                    "Ngày trả hàng không được nhỏ hơn ngày nhập kho gốc.");

            var branch = importDoc.Branch;
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            // ── 2. Lọc bỏ dòng Quantity == 0 ─────────────────────────────────
            var activeDetails = dto.Details.Where(d => d.Quantity > 0).ToList();
            if (!activeDetails.Any())
                throw new MenuGoException(ErrorCodes.DocumentInvalidQuantity,
                    "Tất cả các dòng chi tiết đều có số lượng bằng 0. Không có gì để trả.");

            if (dto.PaidAmount > 0 && (dto.PaidAmount < 1m || dto.PaidAmount % 1m != 0))
                throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice,
                    "Số tiền NCC thực nhận phải là số nguyên lớn hơn hoặc bằng 1.");

            // ── 3. Validate từng dòng chi tiết theo ImportDocumentDetailId ────
            var validatedLines = new List<(ReturnDocumentDetailDto dto, DocumentDetail importDetail)>();

            foreach (var detailDto in activeDetails)
            {
                if (detailDto.UnitPrice < 1m || detailDto.UnitPrice % 1m != 0)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice,
                        $"Đơn giá hoàn trả ({detailDto.UnitPrice}) phải là số nguyên lớn hơn hoặc bằng 1.");

                // 3a. Load ImportDocumentDetail kèm BInventory, UnitConversion
                var importDetail = await _repo.GetImportDocumentDetailAsync(detailDto.ImportDocumentDetailId);


                if (importDetail == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory,
                        $"Không tìm thấy dòng chi tiết nhập kho Id={detailDto.ImportDocumentDetailId}.");

                // 3b. Kiểm tra dòng này thuộc đúng phiếu nhập gốc
                if (importDetail.DocumentId != dto.ImportDocumentId)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory,
                        $"Dòng chi tiết Id={detailDto.ImportDocumentDetailId} không thuộc phiếu nhập Id={dto.ImportDocumentId}.");

                // 3c. Tính RemainingQuantity = ImportDetail.Quantity - SUM(active returns)
                var activeReturns = await _repo.GetReturnedDetailsByImportDetailIdAsync(importDetail.Id);
                decimal alreadyReturned = activeReturns.Sum(r => r.Quantity);
                decimal remainingQuantity = importDetail.Quantity - alreadyReturned;

                if (detailDto.Quantity > remainingQuantity)
                    throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded,
                        $"Số lượng trả vượt quá giới hạn cho dòng Id={importDetail.Id}. " +
                        $"Số lượng nhập: {importDetail.Quantity}, Đã trả: {alreadyReturned}, " +
                        $"Còn được phép trả: {remainingQuantity}.");

                validatedLines.Add((detailDto, importDetail));
            }

            // ── 4. Tổng hợp BInventoryId (loại trùng) để load ledger context ─
            var binvIds = validatedLines
                .Select(x => x.importDetail.BInventoryId)
                .Distinct()
                .ToList();

            var binvs = await _repo.GetBInventoriesAsync(branch.Id, binvIds);
            if (binvs.Count != binvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory,
                    "Một hoặc nhiều sản phẩm không tồn tại trong kho chi nhánh này.");

            // Stock Audit Lock Validation (ERR_STOCK_AUDIT_LOCKED)
            await ValidateStockAuditLockAsync(binvIds, dto.BusinessDate);

            // ── 5. Tính tổng tiền hoàn trả & build Document entity ────────────
            decimal rawTotalRefund = validatedLines.Sum(x => x.dto.Quantity * x.dto.UnitPrice);
            decimal totalRefundAmount = Math.Round(rawTotalRefund, 0, MidpointRounding.AwayFromZero);

            if (dto.PaidAmount > totalRefundAmount)
            {
                throw new MenuGoException(ErrorCodes.DocumentAmountPaidExceedsAmountDue,
                    $"Số tiền đã nhận ({dto.PaidAmount:N0} ₫) không được vượt quá tổng số tiền hoàn trả của NCC ({totalRefundAmount:N0} ₫).");
            }

            var now = DateTime.UtcNow;
            string code = $"RE{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day,
                                               rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = branch.Id,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Return,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount = totalRefundAmount,
                ParentDocumentId = importDoc.Id,
                CreatedBy = createdBy ?? dto.CreatedByAccountId,
                CreatedAt = now
            };

            // ── 6. Load ledger context ────────────────────────────────────────
            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(binvIds, exactSecondDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(binvIds, exactSecondDate);
            var afterLedgersDict = afterLedgers
                .GroupBy(l => l.BInventoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Running state per BInventoryId (khởi tạo từ prior ledger hoặc 0)
            var runningState = new Dictionary<long, (decimal qty, decimal totalValue, decimal avgCost, decimal leftOver)>();
            foreach (var binvId in binvIds)
            {
                var prior = beforeLedgers.FirstOrDefault(l => l.BInventoryId == binvId);
                decimal pq  = prior?.RunningQuantity    ?? 0;
                decimal pav = prior?.RunningAverageCost ?? 0;
                decimal plo = prior?.RunningLeftOver     ?? 0;
                runningState[binvId] = (pq, (pq * pav) + plo, pav, plo);
            }

            // ── 6b. KIỂM TRA TỒN KHO tại thời điểm BusinessDate ─────────────
            // Gom tổng systemQuantity cần trả theo từng BInventoryId,
            // sau đó so sánh với RunningQuantity tại thời điểm được chọn.
            var requiredQtyByBinv = validatedLines
                .GroupBy(x => x.importDetail.BInventoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.dto.Quantity * (x.importDetail.UnitConversion?.ConversionPoint ?? 1m))
                );

            foreach (var (binvId, requiredQty) in requiredQtyByBinv)
            {
                decimal availableQty = runningState[binvId].qty;
                if (requiredQty > availableQty)
                {
                    var binvName = binvs.FirstOrDefault(b => b.Id == binvId)?.Product?.Name ?? $"BInventoryId={binvId}";
                    throw new MenuGoException(ErrorCodes.DocumentReturnInsufficientInventory,
                        $"Tồn kho sản phẩm [{binvName}] không đủ tại ngày {exactSecondDate:dd/MM/yyyy}. " +
                        $"Tồn hiện tại: {availableQty:0.###}, Cần trả: {requiredQty:0.###}.");
                }
            }

            // ── 7. Build DocumentDetails & InventoryLedgers ───────────────────

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();

            foreach (var (detailDto, importDetail) in validatedLines)
            {
                // Kế thừa UnitConversion từ phiếu nhập gốc
                decimal conversionPoint = importDetail.UnitConversion?.ConversionPoint ?? 1m;
                decimal systemQuantity  = detailDto.Quantity * conversionPoint; // base unit

                var docDetail = new DocumentDetail
                {
                    BInventoryId     = importDetail.BInventoryId,
                    FatherId         = importDetail.Id,                  // Trỏ đến ImportDocumentDetailId
                    UnitConversionId = importDetail.UnitConversionId,    // Kế thừa đơn vị từ nhập gốc
                    Quantity         = detailDto.Quantity,
                    SystemQuantity   = systemQuantity,
                    UnitPrice        = detailDto.UnitPrice,
                    Note             = detailDto.Note,
                    CreatedAt        = now
                };
                details.Add(docDetail);

                // Tính ledger — Return là outflow (giảm tồn kho)
                var (runQ, runTV, runAvg, runLO) = runningState[importDetail.BInventoryId];

                var ledger = new InventoryLedger
                {
                    BInventoryId        = importDetail.BInventoryId,
                    BusinessDate        = exactSecondDate,
                    DocumentType        = DocumentType.Return,
                    QuantityDelta       = -systemQuantity,   // Xuất kho (âm)
                    InventoryValueDelta = 0,
                    RunningQuantity     = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost  = 0,
                    RunningLeftOver     = 0,
                    UnitCost            = runAvg,
                    CreatedAt           = now
                };

                RecalculateLedgerState(ledger, runQ, runTV, runAvg, runLO);

                // Cập nhật running state cho dòng kế tiếp cùng BInventoryId
                runningState[importDetail.BInventoryId] = (
                    ledger.RunningQuantity,
                    ledger.RunningInventoryValue,
                    ledger.RunningAverageCost,
                    ledger.RunningLeftOver
                );

                ledgersToCreate.Add(ledger);
            }

            // ── 8. Ripple-recalculate subsequent ledgers & update BInventories ─
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs    = new List<BInventory>();

            foreach (var binvId in binvIds)
            {
                var (runQ, runTV, runAvg, runLO) = runningState[binvId];

                if (afterLedgersDict.TryGetValue(binvId, out var subsequentLedgers))
                {
                    foreach (var ledger in subsequentLedgers)
                    {
                        RecalculateLedgerState(ledger, runQ, runTV, runAvg, runLO);
                        runQ   = ledger.RunningQuantity;
                        runTV  = ledger.RunningInventoryValue;
                        runAvg = ledger.RunningAverageCost;
                        runLO  = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                var binv = binvs.First(b => b.Id == binvId);
                binv.Quantity = runQ;
                binv.Avg      = runAvg;
                binv.LeftOver = runLO;
                updatedBInvs.Add(binv);
            }

            // ── 9. DocumentPartner: ghi nhận công nợ NCC (tổng tiền phải trả) ─
            var supplierPartner = importDoc.DocumentPartners
                .FirstOrDefault(dp => dp.Partner?.Type == PartnerType.Supplier)
                ?? importDoc.DocumentPartners.FirstOrDefault();

            var docPartners = new List<DocumentPartner>();

            if (supplierPartner != null)
            {
                docPartners.Add(new DocumentPartner
                {
                    PartnerId = supplierPartner.PartnerId,
                    Cost      = totalRefundAmount,   // Tổng công nợ NCC phải hoàn trả
                    Note      = dto.Note ?? $"Hoàn tiền từ phiếu trả hàng nhập {code}",
                    CreatedAt = now
                });
            }

            // ── 10. CashFlow: chỉ tạo khi PaidAmount > 0 ─────────────────────
            var cashFlows       = new List<CashFlow>();
            var cashFlowDetails = new List<CashFlowDetail>();

            if (dto.PaidAmount > 0)
            {
                var cf = new CashFlow
                {
                    BranchId     = branch.Id,
                    Code         = $"CF-{code}-1",
                    BusinessDate = exactSecondDate,
                    Direction    = CashFlowDirection.Inflow,       // Thu tiền vào
                    Status       = CashFlowStatus.Completed,
                    TotalAmount  = dto.PaidAmount,                 // Tiền NCC đã thực trả
                    CreatedAt    = now
                };
                cashFlows.Add(cf);

                // Dòng hoàn tiền chính (Refund)
                var cfd = new CashFlowDetail
                {
                    PartnerId     = supplierPartner?.PartnerId,
                    Type          = CashFlowDetailType.Refund,     // Loại: hoàn tiền
                    PaymentMethod = dto.PaymentMethod,
                    Amount        = dto.PaidAmount,
                    Note          = $"Thu tiền hoàn từ NCC – phiếu trả {code}"
                };
                cashFlowDetails.Add(cfd);

                // Dòng điều chỉnh làm tròn (Rounding)
                // RoundingValue = rawTotalRefund - totalRefundAmount (rounded)
                // Return Import (Inflow): Dương → Chi phí khác (-Lãi); Âm → Thu nhập khác (+Lãi)
                decimal roundingValue = rawTotalRefund - totalRefundAmount;
                if (roundingValue != 0m)
                {
                    cashFlowDetails.Add(new CashFlowDetail
                    {
                        PartnerId     = supplierPartner?.PartnerId,
                        Type          = CashFlowDetailType.Rounding,
                        PaymentMethod = dto.PaymentMethod,
                        Amount        = roundingValue,
                        Note          = $"Điều chỉnh làm tròn phiếu trả hàng {code}: " +
                                       $"Giá gốc {rawTotalRefund:N} ₫ → Làm tròn {totalRefundAmount:N0} ₫ " +
                                       $"(Chênh lệch {roundingValue:+#,##0.##;-#,##0.##} ₫)"
                    });
                }
            }

            // ── 11. Execute trong transaction ─────────────────────────────────
            return await _repo.ExecuteReturnTransactionAsync(
                document, details, docPartners, updatedBInvs,
                ledgersToCreate, ledgersToUpdate,
                cashFlows, cashFlowDetails);
        }

        // ──────────────────────────────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────────────────────────────
        public async Task DeleteReturnDocumentAsync(long documentId, long? deletedBy = null)
        {
            // Kế thừa từ DocumentServiceBase:
            // - Soft delete Document + tất cả DocumentDetails
            // - Soft delete CashFlows liên quan
            // - Ripple-recalculate InventoryLedger
            // - Cập nhật BInventory running state
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}

