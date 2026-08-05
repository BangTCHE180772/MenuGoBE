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
    public class ImportDocumentService : DocumentServiceBase, IImportDocumentService
    {
        private new readonly IImportDocumentRepository _repo;

        // 10 tỷ đồng
        private const decimal MaxTotalAmount = 10_000_000_000m;

        public ImportDocumentService(IImportDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        // ──────────────────────────────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────────────────────────────
        public async Task<DocumentEntity> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null)
        {
            // 1. Validate branch & posting lock date
            var branch = await _repo.GetBranchAsync(dto.BranchId);
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            // 2. Load partners & validate: đúng 1 Supplier, 0..n Transport/Other, cấm Customer, không cho phép trùng lặp
            var partnerIds = dto.Partners.Select(p => p.PartnerId).Distinct().ToList();
            if (partnerIds.Count != dto.Partners.Count)
                throw new MenuGoException(ErrorCodes.DocumentDuplicatePartner);

            var partners   = await _repo.GetPartnersAsync(partnerIds);

            Partner? GetPartner(long id) => partners.FirstOrDefault(p => p.Id == id);

            var supplierDtos   = dto.Partners.Where(dp => GetPartner(dp.PartnerId)?.Type == PartnerType.Supplier).ToList();
            var customerDtos   = dto.Partners.Where(dp => GetPartner(dp.PartnerId)?.Type == PartnerType.Customer).ToList();
            var extraDtos      = dto.Partners.Where(dp =>
                                     GetPartner(dp.PartnerId)?.Type == PartnerType.Transporter ||
                                     GetPartner(dp.PartnerId)?.Type == PartnerType.Other).ToList();

            if (supplierDtos.Count != 1)
                throw new MenuGoException(ErrorCodes.DocumentExactlyOneSupplierRequired);
            if (customerDtos.Any())
                throw new MenuGoException(ErrorCodes.DocumentGuestNotAllowedInImport);

            // Transport / Other phải khai rõ AmountDue và phải là số nguyên >= 0
            foreach (var ep in extraDtos)
            {
                if (!ep.AmountDue.HasValue || ep.AmountDue.Value < 0)
                    throw new MenuGoException(ErrorCodes.DocumentAmountDueRequiredForNonSupplier,
                        $"Đối tác {GetPartner(ep.PartnerId)?.Name} (Transport/Other) phải khai báo AmountDue >= 0.");

                if (ep.AmountDue.Value % 1m != 0)
                    throw new MenuGoException(ErrorCodes.DocumentAmountDueRequiredForNonSupplier,
                        $"Số tiền phải trả (AmountDue) cho đối tác {GetPartner(ep.PartnerId)?.Name} (Transport/Other) phải là số nguyên.");
            }

            // 3. Load BInventories (distinct IDs) & validate product types
            var binvIds = dto.Details.Select(d => d.BInventoryId).Distinct().ToList();
            var binvs   = await _repo.GetBInventoriesAsync(dto.BranchId, binvIds);

            if (binvs.Count != binvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory);

            foreach (var binv in binvs)
            {
                if (binv.Product.Type == ProductType.Manufactured || binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport,
                        $"Không thể nhập kho sản phẩm [{binv.Product.Name}] do thuộc loại Manufactured hoặc Processed.");
            }

            // 3b. Stock Audit Lock Validation (ERR_STOCK_AUDIT_LOCKED)
            await ValidateStockAuditLockAsync(binvIds, dto.BusinessDate);

            // 4. Load UnitConversions for details that specify one
            var ucIds = dto.Details
                .Where(d => d.UnitConversionId.HasValue)
                .Select(d => d.UnitConversionId!.Value)
                .Distinct().ToList();

            var unitConversions = await _repo.GetUnitConversionsByIdsAsync(ucIds);

            foreach (var d in dto.Details.Where(d => d.UnitConversionId.HasValue))
            {
                if (!unitConversions.Any(u => u.Id == d.UnitConversionId!.Value))
                    throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound,
                        $"UnitConversion Id={d.UnitConversionId} không tồn tại.");
            }

            // 5. Tính tiền hàng thực tế từng dòng (giữ nguyên thập phân để tính giá trị kho và phân bổ)
            var rawLineGoodsValues = dto.Details
                .Select(d => d.Quantity * d.UnitPrice)
                .ToList();
            decimal rawTotalGoodsValue = rawLineGoodsValues.Sum();

            // 6. Khoản phải trả NCC (supplierAmountDue) làm tròn thành số nguyên theo AwayFromZero
            decimal supplierAmountDue   = Math.Round(rawTotalGoodsValue, 0, MidpointRounding.AwayFromZero);
            decimal extraAmountsDue     = extraDtos.Sum(ep => Math.Round(ep.AmountDue!.Value, 0, MidpointRounding.AwayFromZero));
            decimal documentTotalAmount = supplierAmountDue + extraAmountsDue;

            if (documentTotalAmount > MaxTotalAmount)
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit,
                    $"Tổng giá trị phiếu nhập ({documentTotalAmount:N0} ₫) vượt quá giới hạn 10 tỷ đồng.");

            // Validate AmountPaid <= AmountDue cho từng đối tác
            foreach (var partnerDto in dto.Partners)
            {
                var partner = GetPartner(partnerDto.PartnerId);
                decimal due = partner?.Type == PartnerType.Supplier ? supplierAmountDue : Math.Round(partnerDto.AmountDue ?? 0m, 0, MidpointRounding.AwayFromZero);
                decimal paid = Math.Round(partnerDto.AmountPaid, 0, MidpointRounding.AwayFromZero);
                if (paid > due)
                {
                    throw new MenuGoException(ErrorCodes.DocumentAmountPaidExceedsAmountDue,
                        $"Số tiền đã trả ({paid:N0} ₫) cho đối tác [{partner?.Name}] không được vượt quá số tiền phải trả ({due:N0} ₫).");
                }
            }

            // 7. Build Document entity
            var now = DateTime.UtcNow;
            string code = $"IM{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

            var rawDate        = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day,
                                               rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId       = dto.BranchId,
                BusinessDate   = exactSecondDate,
                Code           = code,
                Type           = DocumentType.Import,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount    = documentTotalAmount,
                CreatedBy      = createdBy ?? dto.CreatedByAccountId,
                CreatedAt      = now
            };

            // 8. Load ledger context for running-state calculation
            var beforeLedgers    = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(binvIds, exactSecondDate);
            var afterLedgers     = await _repo.GetLedgersAfterDateAsync(binvIds, exactSecondDate);
            var afterLedgersDict = afterLedgers
                .GroupBy(l => l.BInventoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Running state per BInventoryId (init from prior ledger or zero)
            var runningState = new Dictionary<long, (decimal qty, decimal totalValue, decimal avgCost, decimal leftOver)>();
            foreach (var binvId in binvIds)
            {
                var prior = beforeLedgers.FirstOrDefault(l => l.BInventoryId == binvId);
                decimal pq  = prior?.RunningQuantity     ?? 0;
                decimal pav = prior?.RunningAverageCost  ?? 0;
                decimal plo = prior?.RunningLeftOver      ?? 0;
                runningState[binvId] = (pq, (pq * pav) + plo, pav, plo);
            }

            // 8.5. Phân bổ chi phí phụ (extraAmountsDue từ Transporter/Other) theo tỷ lệ giá trị tiền hàng thực tế
            var allocatedExtraCosts = new decimal[dto.Details.Count];
            if (extraAmountsDue > 0 && dto.Details.Count > 0)
            {
                if (rawTotalGoodsValue > 0)
                {
                    decimal runningAllocatedSum = 0m;
                    int maxValIndex = 0;
                    decimal maxVal = -1m;

                    for (int i = 0; i < dto.Details.Count; i++)
                    {
                        decimal rawLineValue = rawLineGoodsValues[i];
                        if (rawLineValue > maxVal)
                        {
                            maxVal = rawLineValue;
                            maxValIndex = i;
                        }

                        decimal rawAllocated = extraAmountsDue * (rawLineValue / rawTotalGoodsValue);
                        decimal roundedAllocated = Math.Max(0m, Math.Round(rawAllocated, 0, MidpointRounding.AwayFromZero));
                        allocatedExtraCosts[i] = roundedAllocated;
                        runningAllocatedSum += roundedAllocated;
                    }

                    // Bù phần lẻ dư (remainder) vào dòng hàng có giá trị lớn nhất
                    decimal remainder = extraAmountsDue - runningAllocatedSum;
                    if (remainder != 0)
                    {
                        allocatedExtraCosts[maxValIndex] = Math.Max(0m, allocatedExtraCosts[maxValIndex] + remainder);
                    }
                }
                else
                {
                    // Nếu tổng tiền hàng bằng 0, chia đều cho các dòng hàng
                    decimal perItem = Math.Max(0m, Math.Round(extraAmountsDue / dto.Details.Count, 0, MidpointRounding.AwayFromZero));
                    decimal runningAllocatedSum = 0m;
                    for (int i = 0; i < dto.Details.Count; i++)
                    {
                        allocatedExtraCosts[i] = perItem;
                        runningAllocatedSum += perItem;
                    }
                    decimal remainder = extraAmountsDue - runningAllocatedSum;
                    if (remainder != 0)
                    {
                        allocatedExtraCosts[dto.Details.Count - 1] = Math.Max(0m, allocatedExtraCosts[dto.Details.Count - 1] + remainder);
                    }
                }
            }

            // 9. Build details & ledgers (one-to-one, parallel lists — allows duplicate BInventoryId)
            var details       = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();

            for (int i = 0; i < dto.Details.Count; i++)
            {
                var dtoDetail = dto.Details[i];
                decimal allocatedExtraCost = allocatedExtraCosts[i];

                // UnitConversion lookup
                UnitConversion? uc = dtoDetail.UnitConversionId.HasValue
                    ? unitConversions.First(u => u.Id == dtoDetail.UnitConversionId!.Value)
                    : null;

                decimal conversionPoint  = uc?.ConversionPoint ?? 1m;
                decimal quantityDelta    = dtoDetail.Quantity * conversionPoint;          // Lưu ledger theo base unit
                
                // Giá trị hàng hóa nhập kho = Giá trị gốc (chưa làm tròn) + Chi phí phụ phân bổ
                decimal goodsValue          = rawLineGoodsValues[i];
                decimal inventoryValueDelta = goodsValue + allocatedExtraCost;
                decimal unitCost            = quantityDelta > 0
                    ? inventoryValueDelta / quantityDelta
                    : 0m;

                var docDetail = new DocumentDetail
                {
                    BInventoryId    = dtoDetail.BInventoryId,
                    UnitConversionId = dtoDetail.UnitConversionId,
                    Quantity        = dtoDetail.Quantity,
                    SystemQuantity  = quantityDelta,
                    UnitPrice       = dtoDetail.UnitPrice,
                    Note            = dtoDetail.Note,
                    CreatedAt       = now
                };
                details.Add(docDetail);

                // Get running state for this BInventory
                var (runQ, runTV, runAvg, runLO) = runningState[dtoDetail.BInventoryId];

                var ledger = new InventoryLedger
                {
                    BInventoryId        = dtoDetail.BInventoryId,
                    BusinessDate        = exactSecondDate,
                    DocumentType        = DocumentType.Import,
                    QuantityDelta       = quantityDelta,
                    InventoryValueDelta = inventoryValueDelta,
                    RunningQuantity     = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost  = 0,
                    RunningLeftOver     = 0,
                    UnitCost            = unitCost,
                    CreatedAt           = now
                };

                RecalculateLedgerState(ledger, runQ, runTV, runAvg, runLO);

                // Update running state for next detail with the same BInventoryId
                runningState[dtoDetail.BInventoryId] = (
                    ledger.RunningQuantity,
                    ledger.RunningInventoryValue,
                    ledger.RunningAverageCost,
                    ledger.RunningLeftOver
                );

                ledgersToCreate.Add(ledger);
            }

            // 10. Ripple-recalculate subsequent ledgers (one pass per BInventoryId)
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

                var binv   = binvs.First(b => b.Id == binvId);
                binv.Quantity  = runQ;
                binv.Avg       = runAvg;
                binv.LeftOver  = runLO;
                updatedBInvs.Add(binv);
            }

            // 11. Build DocumentPartners
            var supplierDto = supplierDtos.First();
            var docPartners = new List<DocumentPartner>();

            // Supplier: Cost = rawTotalGoodsValue (nguyên si nợ gốc chưa làm tròn)
            docPartners.Add(new DocumentPartner
            {
                PartnerId  = supplierDto.PartnerId,
                Cost       = rawTotalGoodsValue,
                Note       = supplierDto.Note,
                CreatedAt  = now
            });

            // Transport / Other: AmountDue do user khai (được đảm bảo là số nguyên)
            foreach (var ep in extraDtos)
            {
                docPartners.Add(new DocumentPartner
                {
                    PartnerId  = ep.PartnerId,
                    Cost       = Math.Round(ep.AmountDue!.Value, 0, MidpointRounding.AwayFromZero),
                    Note       = ep.Note,
                    CreatedAt  = now
                });
            }

            // 12. Build CashFlows — Gom tất cả khoản thanh toán và phần dư lẻ làm tròn vào DUY NHẤT 1 phiếu chi CashFlow
            var allPartnerDtos = new List<ImportDocumentPartnerDto> { supplierDto }.Concat(extraDtos).ToList();
            var cashFlows        = new List<CashFlow>();
            var cashFlowDetails  = new List<CashFlowDetail>();
            var cashFlowPartnerIndexes = new List<int>();

            decimal totalPaid = allPartnerDtos.Sum(p => p.AmountPaid);
            decimal roundingValue = rawTotalGoodsValue - supplierAmountDue;

            // Tạo CashFlow nếu có chi tiền (totalPaid > 0) HOẶC có chênh lệch làm tròn Supplier (roundingValue != 0m)
            if (totalPaid > 0 || roundingValue != 0m)
            {
                var cf = new CashFlow
                {
                    BranchId     = dto.BranchId,
                    Code         = $"CF-{code}-1",
                    BusinessDate = exactSecondDate,
                    Direction    = CashFlowDirection.Outflow,
                    Status       = CashFlowStatus.Completed,
                    TotalAmount  = totalPaid,
                    CreatedAt    = now
                };
                cashFlows.Add(cf);

                for (int i = 0; i < allPartnerDtos.Count; i++)
                {
                    var partnerDto = allPartnerDtos[i];
                    decimal amountPaid = partnerDto.AmountPaid;

                    if (amountPaid > 0)
                    {
                        var cfd = new CashFlowDetail
                        {
                            PartnerId     = partnerDto.PartnerId,
                            Type          = CashFlowDetailType.Payment,
                            PaymentMethod = PaymentMethod.Cash,
                            Amount        = amountPaid,
                            Note          = $"Thanh toán cho phiếu nhập kho {code} – {GetPartner(partnerDto.PartnerId)?.Name}"
                        };
                        cashFlowDetails.Add(cfd);
                        cashFlowPartnerIndexes.Add(i);
                    }

                    // Dòng điều chỉnh làm tròn (Rounding) — sinh cho Supplier (i == 0) nếu có phần dư lẻ (kể cả khi chưa thanh toán)
                    if (i == 0 && roundingValue != 0m)
                    {
                        cashFlowDetails.Add(new CashFlowDetail
                        {
                            PartnerId     = partnerDto.PartnerId,
                            Type          = CashFlowDetailType.Rounding,
                            PaymentMethod = PaymentMethod.Cash,
                            Amount        = roundingValue,
                            Note          = $"Điều chỉnh làm tròn phiếu nhập {code}: " +
                                           $"Giá gốc {rawTotalGoodsValue:N} ₫ → Làm tròn {supplierAmountDue:N0} ₫ " +
                                           $"(Chênh lệch {roundingValue:+#,##0.##;-#,##0.##} ₫)"
                        });
                        cashFlowPartnerIndexes.Add(i);
                    }
                }
            }

            // 13. Execute in transaction
            var result = await _repo.ExecuteImportTransactionAsync(
                document, details, docPartners, updatedBInvs,
                ledgersToCreate, ledgersToUpdate,
                cashFlows, cashFlowDetails,
                cashFlowPartnerIndexes);

            return result;
        }

        // ──────────────────────────────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────────────────────────────
        public async Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }

        public async Task<DocumentResponseDto> GetImportReturnDetailsAsync(long importDocumentId)
        {
            var importDoc = await _repo.GetDocumentWithDetailsAsync(importDocumentId);
            if (importDoc == null || importDoc.Type != DocumentType.Import || importDoc.IsDeleted)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound);
            }

            var returnedDetails = await _repo.GetReturnedDetailsByImportIdAsync(importDocumentId);
            var returnedQtyMap = returnedDetails
                .GroupBy(d => d.BInventoryId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

            var returnDocs = await _repo.GetReturnDocumentsByImportIdAsync(importDocumentId);

            var docDto = DocumentService.MapToDto(importDoc);

            foreach (var detail in docDto.Details)
            {
                var alreadyReturned = returnedQtyMap.TryGetValue(detail.BInventoryId, out var qty) ? qty : 0m;
                detail.AlreadyReturnedQuantity = alreadyReturned;
                detail.MaxReturnQuantity = Math.Max(0m, detail.Quantity - alreadyReturned);
            }

            docDto.PreviousReturns = returnDocs.Select(DocumentService.MapToDto).ToList();

            return docDto;
        }
    }
}
