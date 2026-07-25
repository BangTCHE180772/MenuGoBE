using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;

namespace MenuGoBE.Service
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;

        public DocumentService(IDocumentRepository repo)
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

            // 4. Cost Calculation
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

            // Details, Cost Allocation & Inventory
            foreach (var detail in dto.Details)
            {
                decimal itemValue = detail.Quantity * detail.UnitPrice;
                decimal itemAllocatedFee = totalGoodsValue > 0 ? (itemValue / totalGoodsValue) * totalExtraCost : 0;
                decimal incomingTotalValue = itemValue + itemAllocatedFee; // Exact un-truncated money paid
                
                decimal trueUnitCost = detail.UnitPrice + (itemAllocatedFee / detail.Quantity);
                trueUnitCost = Math.Truncate(trueUnitCost * 1000000m) / 1000000m;

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

                // --- STATE BEFORE IMPORT ---
                var priorLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == detail.BInventoryId);
                decimal runningQuantity = priorLedger?.RunningQuantity ?? 0;
                decimal runningAvg = priorLedger?.RunningAverageCost ?? 0;
                decimal runningLeftOver = priorLedger?.RunningLeftOver ?? 0;

                // --- APPLY IMPORT ---
                decimal currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;
                decimal newTotalValue = currentTotalValue + incomingTotalValue;
                
                runningQuantity += detail.Quantity;
                if (runningQuantity > 0)
                {
                    runningAvg = Math.Truncate((newTotalValue / runningQuantity) * 1000000m) / 1000000m;
                    runningLeftOver = newTotalValue - (runningQuantity * runningAvg);
                }
                else
                {
                    runningAvg = 0;
                    runningLeftOver = newTotalValue;
                }

                // Create Import Ledger
                var newLedger = new InventoryLedger
                {
                    BInventoryId = binv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = detail.Quantity,
                    InventoryValueDelta = incomingTotalValue,
                    RunningQuantity = runningQuantity,
                    RunningInventoryValue = newTotalValue,
                    RunningAverageCost = runningAvg,
                    RunningLeftOver = runningLeftOver,
                    UnitCost = trueUnitCost,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };
                ledgersToCreate.Add(newLedger);

                // --- RIPPLE RECALCULATE SUBSEQUENT LEDGERS ---
                if (afterLedgersDict.TryGetValue(binv.Id, out var subsequentLedgers))
                {
                    foreach (var ledger in subsequentLedgers)
                    {
                        currentTotalValue = (runningQuantity * runningAvg) + runningLeftOver;

                        if (ledger.DocumentType == DocumentType.Import || ledger.DocumentType == DocumentType.Return)
                        {
                            // Inflow with explicit value
                            newTotalValue = currentTotalValue + ledger.InventoryValueDelta;
                            runningQuantity += ledger.QuantityDelta;
                        }
                        else
                        {
                            // Outflow (QuantityDelta is negative) -> recalculate outgoing value based on new MAC
                            decimal outgoingValue = ledger.QuantityDelta * runningAvg; 
                            ledger.InventoryValueDelta = outgoingValue; 
                            newTotalValue = currentTotalValue + outgoingValue;
                            runningQuantity += ledger.QuantityDelta;
                        }

                        if (runningQuantity > 0)
                        {
                            runningAvg = Math.Truncate((newTotalValue / runningQuantity) * 1000000m) / 1000000m;
                            runningLeftOver = newTotalValue - (runningQuantity * runningAvg);
                        }
                        else
                        {
                            runningAvg = 0;
                            runningLeftOver = newTotalValue;
                        }

                        ledger.RunningQuantity = runningQuantity;
                        ledger.RunningAverageCost = runningAvg;
                        ledger.RunningLeftOver = runningLeftOver;
                        ledger.RunningInventoryValue = newTotalValue;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                // Finalize BInventory state
                binv.Quantity = runningQuantity;
                binv.Avg = runningAvg;
                binv.LeftOver = runningLeftOver;
                updatedBInvs.Add(binv);
            }

            // Partners & CashFlows
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

            // Create CashFlows
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
    }
}
