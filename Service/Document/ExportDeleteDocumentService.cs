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
    public class ExportDeleteDocumentService : DocumentServiceBase, IExportDeleteDocumentService
    {
        private new readonly IExportDeleteDocumentRepository _repo;

        public ExportDeleteDocumentService(IExportDeleteDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DocumentEntity> CreateExportDeleteDocumentAsync(ExportDeleteDocumentCreateDto dto, long? createdBy = null)
        {
            var branch = await _repo.GetBranchAsync(dto.BranchId);
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            var binvIds = dto.Details.Select(d => d.BInventoryId).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesAsync(dto.BranchId, binvIds);

            if (binvs.Count != binvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory);

            foreach (var binv in binvs)
            {
                if (binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể xuất hủy sản phẩm [{binv.Product.Name}] do thuộc loại Processed.");
            }

            var now = DateTime.UtcNow;
            string code = $"EX{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = dto.BranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Export,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount = 0,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(binvIds, document.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(binvIds, document.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();
            decimal totalExportValue = 0;

            foreach (var detail in dto.Details)
            {
                var binv = binvs.First(b => b.Id == detail.BInventoryId);

                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == detail.BInventoryId);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;

                // RÀNG BUỘC PHƯƠNG ÁN 2: Bắt buộc số lượng xuất hủy không được lớn hơn số tồn tại mốc thời gian xuất
                if (runningQuantity < detail.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Không thể xuất hủy sản phẩm [{binv.Product.Name}]. Số lượng xuất hủy ({detail.Quantity}) lớn hơn số tồn kho tại mốc thời gian xuất ({runningQuantity}).");
                }

                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = detail.BInventoryId,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = runningAvg,
                    Note = detail.Note,
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var newLedger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = -detail.Quantity,
                    InventoryValueDelta = 0,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = runningAvg,
                    DocumentDetail = docDetail,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(newLedger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);

                runningQuantity = newLedger.RunningQuantity;
                currentTotalValue = newLedger.RunningInventoryValue;
                runningAvg = newLedger.RunningAverageCost;
                runningLeftOver = newLedger.RunningLeftOver;
                totalExportValue += Math.Abs(newLedger.InventoryValueDelta);

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

            document.TotalAmount = totalExportValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        public async Task DeleteExportDeleteDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
