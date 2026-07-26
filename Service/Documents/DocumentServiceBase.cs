using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;

namespace MenuGoBE.Service.Documents
{
    public abstract class DocumentServiceBase
    {
        protected readonly IDocumentRepository _repo;

        protected DocumentServiceBase(IDocumentRepository repo)
        {
            _repo = repo;
        }

        protected async Task DeleteDocumentAsync(long documentId, long? deletedBy = null)
        {
            var document = await _repo.GetDocumentWithDetailsAsync(documentId);
            if (document == null)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy chứng từ.");

            if (document.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentAlreadyDeleted, "Chứng từ đã bị xóa trước đó.");

            if (document.Branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (document.Type == DocumentType.Import)
            {
                var hasReturn = await _repo.HasReturnDocumentsAsync(documentId);
                if (hasReturn)
                    throw new MenuGoException(ErrorCodes.DocumentCannotDeleteHasReturn, "Không thể xóa phiếu nhập vì đã có phiếu trả hàng tham chiếu đến nó.");
            }

            // 1. Validation: PostingLockDate
            if (document.Branch.PostingLockDate.HasValue && document.BusinessDate.Date <= document.Branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Không thể xóa chứng từ do ngày chứng từ nằm trước hoặc bằng ngày khóa sổ.");

            // 2. Prepare soft delete for Document
            document.IsDeleted = true;
            document.DeletedBy = deletedBy;
            document.DeletedAt = DateTime.UtcNow;
            document.Status = DocumentStatus.Cancelled;

            // 3. Prepare soft delete for associated CashFlows
            var cashFlows = await _repo.GetCashFlowsByDocumentIdAsync(documentId);
            foreach (var cf in cashFlows)
            {
                cf.IsDeleted = true;
                cf.DeletedBy = deletedBy;
                cf.DeletedAt = DateTime.UtcNow;
                cf.Status = CashFlowStatus.Cancelled;
            }

            // 4. Find affected InventoryLedgers and BInventories
            var binvIds = document.DocumentDetails.Select(d => d.BInventoryId).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesByIdsAsync(binvIds);
            var ledgersToDelete = await _repo.GetLedgersByDocumentIdAsync(documentId);

            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();

            // 5. Ripple Recalculate
            foreach (var binv in binvs)
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
                updatedBInvs.Add(binv);
            }

            // 6. Execute Transaction
            await _repo.ExecuteDeleteTransactionAsync(document, ledgersToDelete, ledgersToUpdate, updatedBInvs, cashFlows);
        }

        protected void RecalculateLedgerState(
            InventoryLedger ledger,
            decimal previousQuantity,
            decimal previousTotalValue,
            decimal previousAvgCost,
            decimal previousLeftOver)
        {
            if (ledger.DocumentType == DocumentType.Check && ledger.DocumentDetail != null)
            {
                ledger.QuantityDelta = ledger.DocumentDetail.Quantity - previousQuantity;
            }

            decimal nextQty = previousQuantity + ledger.QuantityDelta;
            decimal valueDelta = 0;

            bool isStockInflow = ledger.DocumentType == DocumentType.Import ||
                                 (ledger.DocumentType == DocumentType.Production && ledger.QuantityDelta > 0) ||
                                 (ledger.DocumentType == DocumentType.Transfer && ledger.QuantityDelta > 0);

            if (isStockInflow)
            {
                valueDelta = ledger.InventoryValueDelta;
            }
            else if (ledger.DocumentType == DocumentType.Check)
            {
                if (nextQty <= 0 && previousQuantity > 0)
                {
                    valueDelta = -previousTotalValue;
                }
                else if (nextQty <= 0 && previousQuantity <= 0)
                {
                    valueDelta = 0;
                }
                else
                {
                    if (previousQuantity <= 0)
                    {
                        valueDelta = nextQty * (ledger.DocumentDetail?.UnitPrice ?? ledger.UnitCost);
                    }
                    else
                    {
                        valueDelta = ledger.QuantityDelta * previousAvgCost;
                    }
                }
                ledger.InventoryValueDelta = valueDelta;
            }
            else
            {
                // Outflow (Export, Return, Transfer Outflow, Production Raw Materials Outflow)
                if (previousQuantity <= 0)
                {
                    valueDelta = 0;
                }
                else if (nextQty <= 0)
                {
                    valueDelta = -previousTotalValue;
                }
                else
                {
                    valueDelta = ledger.QuantityDelta * previousAvgCost;
                }
                ledger.InventoryValueDelta = valueDelta;
            }

            decimal newTotalValue = previousTotalValue + valueDelta;
            decimal newAvgCost = previousAvgCost;
            decimal newLeftOver = previousLeftOver;

            if (nextQty > 0)
            {
                if (previousQuantity <= 0)
                {
                    // MAC Rule for negative recovery: MAC = Total Incoming Value / Incoming Quantity (V_in / Q_in)
                    if (ledger.QuantityDelta > 0 && ledger.InventoryValueDelta > 0)
                    {
                        decimal incomingUnitCost = ledger.InventoryValueDelta / ledger.QuantityDelta;
                        newTotalValue = nextQty * incomingUnitCost;
                        newAvgCost = Math.Truncate(incomingUnitCost * 1000000m) / 1000000m;
                        newLeftOver = newTotalValue - (nextQty * newAvgCost);
                    }
                    else if (ledger.DocumentType == DocumentType.Check)
                    {
                        newTotalValue = valueDelta;
                        newAvgCost = nextQty > 0 ? Math.Truncate((newTotalValue / nextQty) * 1000000m) / 1000000m : previousAvgCost;
                        newLeftOver = newTotalValue - (nextQty * newAvgCost);
                    }
                }
                else if (isStockInflow)
                {
                    newAvgCost = Math.Truncate((newTotalValue / nextQty) * 1000000m) / 1000000m;
                    newLeftOver = newTotalValue - (nextQty * newAvgCost);
                }
                else
                {
                    // Outflow (Export, Return, Transfer Outflow, Production Raw Material Outflow): Avg cost does NOT change!
                    newAvgCost = previousAvgCost;
                    newLeftOver = previousLeftOver;
                    newTotalValue = (nextQty * newAvgCost) + newLeftOver;
                }
            }
            else
            {
                newTotalValue = 0;
                newLeftOver = previousQuantity > 0 ? previousLeftOver : 0;
                if (previousQuantity <= 0)
                {
                    newAvgCost = 0;
                    ledger.UnitCost = 0;
                }
            }

            ledger.RunningQuantity = nextQty;
            ledger.RunningAverageCost = newAvgCost;
            ledger.RunningLeftOver = newLeftOver;
            ledger.RunningInventoryValue = newTotalValue;
        }
    }
}
