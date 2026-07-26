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

namespace MenuGoBE.Service.Document
{
    public class CheckDocumentService : DocumentServiceBase, ICheckDocumentService
    {
        private new readonly ICheckDocumentRepository _repo;

        public CheckDocumentService(ICheckDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<Document> CreateCheckDocumentAsync(CheckDocumentCreateDto dto, long? createdBy = null)
        {
            var adjDto = new AdjustmentDocumentCreateDto
            {
                BranchId = dto.BranchId,
                BusinessDate = dto.BusinessDate,
                Details = dto.Details.Select(d => new AdjustmentDocumentDetailDto
                {
                    BInventoryId = d.BInventoryId,
                    ActualQuantity = d.ActualQuantity,
                    UnitPrice = d.UnitPrice,
                    Note = d.Note
                }).ToList()
            };
            return await CreateAdjustmentDocumentAsync(adjDto, createdBy);
        }

        public async Task<Document> CreateAdjustmentDocumentAsync(AdjustmentDocumentCreateDto dto, long? createdBy = null)
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
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể kiểm kho sản phẩm [{binv.Product.Name}] do thuộc loại Processed.");
            }

            var now = DateTime.UtcNow;
            string code = $"AD{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new Document
            {
                BranchId = dto.BranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Check,
                Status = DocumentStatus.Completed,
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
            decimal totalAdjustmentValue = 0;

            foreach (var detail in dto.Details)
            {
                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == detail.BInventoryId);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;
                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                decimal quantityDelta = detail.ActualQuantity - runningQuantity;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = detail.BInventoryId,
                    Quantity = detail.ActualQuantity,
                    SystemQuantity = runningQuantity,
                    UnitPrice = detail.UnitPrice ?? runningAvg,
                    Note = detail.Note,
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var binv = binvs.First(b => b.Id == detail.BInventoryId);

                var newLedger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = quantityDelta,
                    InventoryValueDelta = 0,
                    RunningQuantity = 0, 
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = detail.UnitPrice ?? runningAvg,
                    DocumentDetail = docDetail,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(newLedger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);
                
                runningQuantity = newLedger.RunningQuantity;
                currentTotalValue = newLedger.RunningInventoryValue;
                runningAvg = newLedger.RunningAverageCost;
                runningLeftOver = newLedger.RunningLeftOver;
                totalAdjustmentValue += Math.Abs(newLedger.InventoryValueDelta);

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
            
            document.TotalAmount = totalAdjustmentValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs, 
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        public async Task DeleteCheckDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
