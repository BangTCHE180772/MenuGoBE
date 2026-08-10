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
/*
namespace MenuGoBE.Service.Document
{
    public class InvoiceDocumentService : DocumentServiceBase, IInvoiceDocumentService
    {
        private new readonly IInvoiceDocumentRepository _repo;

        public InvoiceDocumentService(IInvoiceDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DocumentEntity> CreateInvoiceAsync(long branchId, List<InvoiceOutflowLineDto> lines, long? createdBy = null)
        {
            var branch = await _repo.GetBranchAsync(branchId);
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            var businessDate = DateTime.UtcNow;
            if (branch.PostingLockDate.HasValue && businessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            // Merge lines targeting the same product so each BInventory is only
            // touched once per invoice (avoids re-reading a stale prior-ledger snapshot).
            var merged = lines
                .GroupBy(l => l.ProductId)
                .Select(g => new InvoiceOutflowLineDto
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Note = g.First().Note
                })
                .ToList();

            var productIds = merged.Select(l => l.ProductId).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesByProductIdsAsync(branchId, productIds);

            if (binvs.Count != productIds.Count)
            {
                var missing = productIds.Except(binvs.Select(b => b.ProductId));
                throw new MenuGoException(
                    ErrorCodes.DocumentProductNotInInventory,
                    $"Một hoặc nhiều sản phẩm/nguyên liệu (ProductId: {string.Join(", ", missing)}) chưa được cấu hình trong kho chi nhánh này.");
            }

            var binvIds = binvs.Select(b => b.Id).ToList();

            // Stock Audit Lock Validation (ERR_STOCK_AUDIT_LOCKED)
            await ValidateStockAuditLockAsync(binvIds, businessDate);

            var now = DateTime.UtcNow;
            string code = $"INV{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            var exactSecondDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = branchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Invoice,
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
            decimal totalInvoiceValue = 0;

            foreach (var line in merged)
            {
                var binv = binvs.First(b => b.ProductId == line.ProductId);

                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == binv.Id);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;

                if (runningQuantity < line.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentInvoiceInsufficientIngredient,
                        $"Không đủ tồn kho [{binv.Product.Name}] để xác nhận món. Tồn hiện tại: {runningQuantity:0.###}, cần dùng: {line.Quantity:0.###}.");
                }

                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = binv.Id,
                    Quantity = line.Quantity,
                    SystemQuantity = line.Quantity,
                    UnitPrice = runningAvg,
                    Note = line.Note ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var newLedger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = -line.Quantity,
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
                totalInvoiceValue += Math.Abs(newLedger.InventoryValueDelta);

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

            document.TotalAmount = totalInvoiceValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }
    }
}
*/