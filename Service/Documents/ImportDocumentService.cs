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

namespace MenuGoBE.Service.Documents
{
    public class ImportDocumentService : DocumentServiceBase, IImportDocumentService
    {
        private new readonly IImportDocumentRepository _repo;

        public ImportDocumentService(IImportDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<DocumentEntity> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null)
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
                if (binv.Product.Type == ProductType.Manufactured || binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể nhập kho sản phẩm [{binv.Product.Name}] do thuộc loại Manufactured hoặc Processed.");
            }

            var partnerIds = dto.Partners.Select(p => p.PartnerId).Distinct().ToList();
            var partners = await _repo.GetPartnersAsync(partnerIds);

            if (partners.Count != partnerIds.Count) //C:\Duos\CSharp\CapstoneProject\MenuGoBE\Service\Documents\ImportDocumentService.cs
                throw new MenuGoException(ErrorCodes.DocumentPartnerNotFound);

            var suppliers = partners.Where(p => p.Type == PartnerType.Supplier).ToList();
            var guests = partners.Where(p => p.Type == PartnerType.Customer).ToList();

            if (suppliers.Count != 1)
                throw new MenuGoException(ErrorCodes.DocumentExactlyOneSupplierRequired);
            if (guests.Any())
                throw new MenuGoException(ErrorCodes.DocumentGuestNotAllowedInImport);

            decimal totalGoodsValue = dto.Details.Sum(d => d.Quantity * d.UnitPrice);
            
            var transAndOthers = dto.Partners.Where(dp => 
                partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Transporter ||
                partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Other).ToList();
            
            decimal totalExtraCost = transAndOthers.Sum(dp => dp.Cost);

            var now = DateTime.UtcNow;
            string code = $"IM{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new DocumentEntity
            {
                BranchId = dto.BranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.None,
                TotalAmount = totalGoodsValue + totalExtraCost,
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
                var binv = binvs.First(b => b.Id == detail.BInventoryId);
                decimal extraCostPerUnit = totalGoodsValue > 0 ? (detail.Quantity * detail.UnitPrice / totalGoodsValue) * totalExtraCost / detail.Quantity : 0;
                decimal actualUnitCost = detail.UnitPrice + extraCostPerUnit;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = detail.BInventoryId,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = actualUnitCost,
                    Note = detail.Note,
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == binv.Id);
                decimal runningQty = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;
                decimal currentTotalVal = (runningQty * runningAvg) + runningLeftOver;

                var ledger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = detail.Quantity,
                    InventoryValueDelta = detail.Quantity * actualUnitCost,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = actualUnitCost,
                    DocumentDetail = docDetail,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(ledger, runningQty, currentTotalVal, runningAvg, runningLeftOver);
                runningQty = ledger.RunningQuantity;
                currentTotalVal = ledger.RunningInventoryValue;
                runningAvg = ledger.RunningAverageCost;
                runningLeftOver = ledger.RunningLeftOver;
                ledgersToCreate.Add(ledger);

                if (afterLedgersDict.TryGetValue(binv.Id, out var subsequentLedgers))
                {
                    foreach (var subLedger in subsequentLedgers)
                    {
                        RecalculateLedgerState(subLedger, runningQty, currentTotalVal, runningAvg, runningLeftOver);
                        runningQty = subLedger.RunningQuantity;
                        currentTotalVal = subLedger.RunningInventoryValue;
                        runningAvg = subLedger.RunningAverageCost;
                        runningLeftOver = subLedger.RunningLeftOver;
                        ledgersToUpdate.Add(subLedger);
                    }
                }

                binv.Quantity = runningQty;
                binv.Avg = runningAvg;
                binv.LeftOver = runningLeftOver;
                updatedBInvs.Add(binv);
            }

            var docPartners = dto.Partners.Select(p => new DocumentPartner
            {
                PartnerId = p.PartnerId,
                Cost = p.Cost,
                Note = p.Note,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            var cashFlows = new List<CashFlow>();
            var cashFlowDetails = new List<CashFlowDetail>();

            foreach (var dp in docPartners)
            {
                var partnerObj = partners.First(p => p.Id == dp.PartnerId);
                var cf = new CashFlow
                {
                    BranchId = dto.BranchId,
                    BusinessDate = document.BusinessDate,
                    Direction = CashFlowDirection.Outflow,
                    TotalAmount = dp.Cost,
                    Status = CashFlowStatus.Completed,
                    //Cre = createdBy,
                    CreatedAt = DateTime.UtcNow
                };
                cashFlows.Add(cf);

                var cfd = new CashFlowDetail
                {
                    Type = CashFlowDetailType.Payment,
                    Amount = dp.Cost,
                    Note = $"Thanh toán cho {partnerObj.Name} phiếu nhập {code}"
                };
                cashFlowDetails.Add(cfd);
            }

            return await _repo.ExecuteImportTransactionAsync(
                document, details, docPartners, updatedBInvs,
                ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }

        public async Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null)
        {
            await base.DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
