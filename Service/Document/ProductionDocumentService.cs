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
    public class ProductionDocumentService : DocumentServiceBase, IProductionDocumentService
    {
        private new readonly IProductionDocumentRepository _repo;

        public ProductionDocumentService(IProductionDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DocumentEntity> CreateProductionDocumentAsync(ProductionDocumentCreateDto dto, long? createdBy = null)
        {
            var branch = await _repo.GetBranchAsync(dto.BranchId);
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            var rawBinvIds = dto.RawMaterials.Select(r => r.BInventoryId).Distinct().ToList();
            var allBinvIds = rawBinvIds.Concat(new[] { dto.FinishedBInventoryId }).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesAsync(dto.BranchId, allBinvIds);

            if (binvs.Count != allBinvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Một hoặc nhiều nguyên vật liệu/thành phẩm không tồn tại trong kho.");

            var finishedBinv = binvs.FirstOrDefault(b => b.Id == dto.FinishedBInventoryId);
            if (finishedBinv == null)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Thành phẩm không tồn tại trong kho.");

            if (finishedBinv.Product.Type != ProductType.Manufactured)
                throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Sản xuất chỉ dành cho sản phẩm thuộc loại Manufactured. Sản phẩm [{finishedBinv.Product.Name}] thuộc loại {finishedBinv.Product.Type}.");

            foreach (var raw in dto.RawMaterials)
            {
                var rawBinv = binvs.FirstOrDefault(b => b.Id == raw.BInventoryId);
                if (rawBinv != null && rawBinv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể xuất nguyên liệu [{rawBinv.Product.Name}] do thuộc loại Processed.");
            }

            var now = DateTime.UtcNow;
            string code = $"PR{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = dto.BranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Production,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount = 0,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(allBinvIds, document.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(allBinvIds, document.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();
            decimal totalConsumedRawMaterialValue = 0;

            // 1. OUTFLOW RAW MATERIALS
            foreach (var raw in dto.RawMaterials)
            {
                var binv = binvs.First(b => b.Id == raw.BInventoryId);

                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == raw.BInventoryId);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;

                // RÀNG BUỘC PHƯƠNG ÁN 2: Bắt buộc phải đủ hàng tồn kho tại mốc thời gian sản xuất
                if (runningQuantity < raw.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Không đủ số lượng nguyên vật liệu [{binv.Product.Name}] tại mốc thời gian sản xuất. Tồn sổ sách hiện tại: {runningQuantity}, Cần xuất: {raw.Quantity}.");
                }

                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = raw.BInventoryId,
                    Quantity = raw.Quantity,
                    SystemQuantity = raw.Quantity,
                    UnitPrice = runningAvg,
                    Note = $"Xuất nguyên vật liệu cho sản xuất: {raw.Note}",
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var newLedger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = -raw.Quantity,
                    InventoryValueDelta = 0,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = runningAvg,
                    DocumentDetail = docDetail,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(newLedger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);

                runningQuantity = newLedger.RunningQuantity;
                currentTotalValue = newLedger.RunningInventoryValue;
                runningAvg = newLedger.RunningAverageCost;
                runningLeftOver = newLedger.RunningLeftOver;
                totalConsumedRawMaterialValue += Math.Abs(newLedger.InventoryValueDelta);

                ledgersToCreate.Add(newLedger);

                if (afterLedgersDict.TryGetValue(binv.Id, out var subsequentLedgers))
                {
                    foreach (var ledger in subsequentLedgers)
                    {
                        RecalculateLedgerState(ledger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);
                        runningQuantity = ledger.RunningQuantity;
                        currentTotalValue = ledger.RunningInventoryValue;
                        runningAvg = ledger.RunningAverageCost;
                        runningLeftOver = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                binv.Quantity = runningQuantity;
                binv.Avg = runningAvg;
                binv.LeftOver = runningLeftOver;
                updatedBInvs.Add(binv);
            }

            // 2. INFLOW FINISHED GOODS
            decimal finishedUnitCost = dto.FinishedQuantity > 0 ? totalConsumedRawMaterialValue / dto.FinishedQuantity : 0;

            var finishedDocDetail = new DocumentDetail
            {
                BInventoryId = finishedBinv.Id,
                Quantity = dto.FinishedQuantity,
                SystemQuantity = dto.FinishedQuantity,
                UnitPrice = finishedUnitCost,
                Note = $"Nhập kho thành phẩm sản xuất: {dto.Note}",
                CreatedAt = DateTime.UtcNow
            };
            details.Add(finishedDocDetail);

            var priorFinishedLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == finishedBinv.Id);
            decimal finishedQty = priorFinishedLedger?.RunningQuantity ?? 0;
            decimal finishedAvg = priorFinishedLedger?.RunningAverageCost ?? 0;
            decimal finishedLeftOver = priorFinishedLedger?.RunningLeftOver ?? 0;
            decimal finishedTotalVal = (finishedQty * finishedAvg) + finishedLeftOver;

            var finishedLedger = new InventoryLedger
            {
                BInventoryId = finishedBinv.Id,
                BusinessDate = document.BusinessDate,
                DocumentType = document.Type,
                QuantityDelta = dto.FinishedQuantity,
                InventoryValueDelta = totalConsumedRawMaterialValue,
                RunningQuantity = 0,
                RunningInventoryValue = 0,
                RunningAverageCost = 0,
                RunningLeftOver = 0,
                UnitCost = finishedUnitCost,
                DocumentDetail = finishedDocDetail,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            RecalculateLedgerState(finishedLedger, finishedQty, finishedTotalVal, finishedAvg, finishedLeftOver);

            finishedQty = finishedLedger.RunningQuantity;
            finishedTotalVal = finishedLedger.RunningInventoryValue;
            finishedAvg = finishedLedger.RunningAverageCost;
            finishedLeftOver = finishedLedger.RunningLeftOver;
            ledgersToCreate.Add(finishedLedger);

            if (afterLedgersDict.TryGetValue(finishedBinv.Id, out var subsequentFinishedLedgers))
            {
                foreach (var ledger in subsequentFinishedLedgers)
                {
                    RecalculateLedgerState(ledger, finishedQty, finishedTotalVal, finishedAvg, finishedLeftOver);
                    finishedQty = ledger.RunningQuantity;
                    finishedTotalVal = ledger.RunningInventoryValue;
                    finishedAvg = ledger.RunningAverageCost;
                    finishedLeftOver = ledger.RunningLeftOver;
                    ledgersToUpdate.Add(ledger);
                }
            }

            finishedBinv.Quantity = finishedQty;
            finishedBinv.Avg = finishedAvg;
            finishedBinv.LeftOver = finishedLeftOver;
            updatedBInvs.Add(finishedBinv);

            document.TotalAmount = totalConsumedRawMaterialValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        public async Task DeleteProductionDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
