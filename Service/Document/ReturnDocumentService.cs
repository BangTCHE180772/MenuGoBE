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

        public async Task<DocumentEntity> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null)
        {
            var importDoc = await _repo.GetDocumentWithDetailsAsync(dto.ImportDocumentId);
            if (importDoc == null || importDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy phiếu nhập gốc hoặc phiếu đã bị xóa.");

            if (importDoc.Type != DocumentType.Import)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Chứng từ tham chiếu không phải là phiếu nhập kho.");

            if (dto.BusinessDate.Date < importDoc.BusinessDate.Date)
                throw new MenuGoException(ErrorCodes.DocumentInvalidReturnBusinessDate, "Ngày trả hàng không được nhỏ hơn ngày nhập kho gốc.");

            var branch = importDoc.Branch;
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            var binvIds = dto.Details.Select(d => d.BInventoryId).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesAsync(branch.Id, binvIds);

            if (binvs.Count != binvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory);

            var previousReturnedDetails = await _repo.GetReturnedDetailsByImportIdAsync(importDoc.Id);
            var returnedQuantitiesDict = previousReturnedDetails
                .GroupBy(d => d.BInventoryId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

            foreach (var detail in dto.Details)
            {
                var importDetail = importDoc.DocumentDetails.FirstOrDefault(d => d.BInventoryId == detail.BInventoryId);
                if (importDetail == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Sản phẩm trả lại không nằm trong phiếu nhập gốc.");

                decimal alreadyReturned = returnedQuantitiesDict.ContainsKey(detail.BInventoryId) 
                    ? returnedQuantitiesDict[detail.BInventoryId] 
                    : 0m;
                    
                decimal remainingQuantity = importDetail.Quantity - alreadyReturned;

                if (detail.Quantity > remainingQuantity)
                {
                    throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Số lượng trả vượt quá giới hạn. Đã nhập: {importDetail.Quantity}, " +
                        $"Đã trả các lần trước: {alreadyReturned}, Còn lại được phép trả: {remainingQuantity}.");
                }
            }

            var supplierPartner = importDoc.DocumentPartners.FirstOrDefault();
            decimal totalRefundValue = dto.Details.Sum(d => d.Quantity * d.UnitPrice);

            var now = DateTime.UtcNow;
            string code = $"RE{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = branch.Id,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Return,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount = totalRefundValue,
                ParentDocumentId = importDoc.Id,
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

            foreach (var detail in dto.Details)
            {
                var docDetail = new DocumentDetail
                {
                    BInventoryId = detail.BInventoryId,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice, 
                    Note = detail.Note,
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var binv = binvs.First(b => b.Id == detail.BInventoryId);

                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == detail.BInventoryId);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;
                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

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
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(newLedger, runningQuantity, currentTotalValue, runningAvg, runningLeftOver);
                
                runningQuantity = newLedger.RunningQuantity;
                currentTotalValue = newLedger.RunningInventoryValue;
                runningAvg = newLedger.RunningAverageCost;
                runningLeftOver = newLedger.RunningLeftOver;

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

            var docPartners = new List<DocumentPartner>();
            var cashFlows = new List<CashFlow>();
            var cashFlowDetails = new List<CashFlowDetail>();

            if (supplierPartner != null && totalRefundValue > 0)
            {
                docPartners.Add(new DocumentPartner
                {
                    PartnerId = supplierPartner.PartnerId,
                    Cost = totalRefundValue,
                    Note = "Hoàn trả tiền từ phiếu trả hàng nhập",
                    CreatedAt = DateTime.UtcNow
                });

                var cf = new CashFlow
                {
                    BranchId = branch.Id,
                    Code = $"CF-{code}-1",
                    BusinessDate = document.BusinessDate,
                    Direction = CashFlowDirection.Inflow,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = totalRefundValue,
                    CreatedAt = DateTime.UtcNow
                };
                cashFlows.Add(cf);
                
                var cfd = new CashFlowDetail
                {
                    PartnerId = supplierPartner.PartnerId,
                    Type = CashFlowDetailType.Refund,
                    PaymentMethod = PaymentMethod.Cash,
                    Amount = totalRefundValue,
                    Note = $"Thu tiền từ phiếu trả hàng nhập {code}"
                };
                cashFlowDetails.Add(cfd);
            }

            return await _repo.ExecuteReturnTransactionAsync(document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }

        public async Task DeleteReturnDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
