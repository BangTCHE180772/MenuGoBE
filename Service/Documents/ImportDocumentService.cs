using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository.Documents;
using MenuGoBE.Interface.Services.Documents;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;

namespace MenuGoBE.Service.Documents
{
    public class ImportDocumentService : DocumentServiceBase, IImportDocumentService
    {
        private new readonly IImportDocumentRepository _repo;

        public ImportDocumentService(IImportDocumentRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public async Task<Document> CreateImportDocumentAsync(ImportDocumentCreateDto dto, long? createdBy = null)
        {
            var branch = await _repo.GetBranchAsync(dto.BranchId);
            if (branch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            // 1. Validation: PostingLockDate
            if (branch.PostingLockDate.HasValue && dto.BusinessDate.Date <= branch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError);

            // 2. Validation: Products
            var binvIds = dto.Details.Select(d => d.BInventoryId).Distinct().ToList();
            var binvs = await _repo.GetBInventoriesAsync(dto.BranchId, binvIds);

            if (binvs.Count != binvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory);

            foreach (var binv in binvs)
            {
                if (binv.Product.Type == ProductType.Manufactured || binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể nhập kho sản phẩm [{binv.Product.Name}] do thuộc loại Manufactured hoặc Processed.");
            }

            // 3. Validation: Partners
            var partnerIds = dto.Partners.Select(p => p.PartnerId).Distinct().ToList();
            var partners = await _repo.GetPartnersAsync(partnerIds);
            
            var suppliers = dto.Partners.Where(dp => partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Supplier).ToList();
            var guests = dto.Partners.Where(dp => partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Customer).ToList();

            if (suppliers.Count != 1)
                throw new MenuGoException(ErrorCodes.DocumentExactlyOneSupplierRequired);
            if (guests.Any())
                throw new MenuGoException(ErrorCodes.DocumentGuestNotAllowedInImport);

            // 4. Cost Allocation
            decimal totalGoodsValue = dto.Details.Sum(d => d.Quantity * d.UnitPrice);
            
            var transAndOthers = dto.Partners.Where(dp => 
                partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Transporter ||
                partners.FirstOrDefault(p => p.Id == dp.PartnerId)?.Type == PartnerType.Other).ToList();
            
            decimal totalExtraCost = transAndOthers.Sum(p => p.Cost);

            var now = DateTime.UtcNow;
            string code = $"IM{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new Document
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
                decimal itemValue = detail.Quantity * detail.UnitPrice;
                decimal itemAllocatedFee = totalGoodsValue > 0 ? (itemValue / totalGoodsValue) * totalExtraCost : 0;
                decimal incomingTotalValue = itemValue + itemAllocatedFee;
                
                decimal trueUnitCost = detail.UnitPrice + (itemAllocatedFee / detail.Quantity);

                var docDetail = new DocumentDetail
                {
                    BInventoryId = detail.BInventoryId,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = trueUnitCost,
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
                    QuantityDelta = detail.Quantity,
                    InventoryValueDelta = incomingTotalValue,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = trueUnitCost,
                    CreatedBy = createdBy,
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

            var supplierDto = suppliers.First();
            docPartners.Add(new DocumentPartner
            {
                PartnerId = supplierDto.PartnerId,
                Cost = totalGoodsValue,
                Note = supplierDto.Note,
                CreatedAt = DateTime.UtcNow
            });

            foreach (var tp in transAndOthers)
            {
                docPartners.Add(new DocumentPartner
                {
                    PartnerId = tp.PartnerId,
                    Cost = tp.Cost,
                    Note = tp.Note,
                    CreatedAt = DateTime.UtcNow
                });
            }

            string cfCodePrefix = $"CF-{code}-";
            int cfIndex = 1;
            foreach (var dp in docPartners)
            {
                var cf = new CashFlow
                {
                    BranchId = dto.BranchId,
                    Code = $"{cfCodePrefix}{cfIndex++}",
                    BusinessDate = document.BusinessDate,
                    Direction = CashFlowDirection.Outflow,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = dp.Cost,
                    CreatedAt = DateTime.UtcNow
                };
                cashFlows.Add(cf);
                
                var cfd = new CashFlowDetail
                {
                    PartnerId = dp.PartnerId,
                    Type = CashFlowDetailType.Payment,
                    PaymentMethod = PaymentMethod.Cash,
                    Amount = dp.Cost,
                    Note = $"Thanh toán cho phiếu nhập kho {code}"
                };
                cashFlowDetails.Add(cfd);
            }

            return await _repo.ExecuteImportTransactionAsync(document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }

        public async Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }
    }
}
