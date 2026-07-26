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

        // ==========================================
        // 1. IMPORT DOCUMENT (Nhập hàng - Type 1)
        // ==========================================
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

            return await _repo.ExecuteDocumentTransactionAsync(document, details, docPartners, updatedBInvs, ledgersToCreate, ledgersToUpdate, cashFlows, cashFlowDetails);
        }

        public async Task DeleteImportDocumentAsync(long documentId, long? deletedBy = null)
        {
            await DeleteDocumentAsync(documentId, deletedBy);
        }

        // ==========================================
        // 2. EXPORT DELETE DOCUMENT (Xuất hủy - Type 2)
        // ==========================================
        public async Task<Document> CreateExportDeleteDocumentAsync(ExportDeleteDocumentCreateDto dto, long? createdBy = null)
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

            var document = new Document
            {
                BranchId = dto.BranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Export,
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
                    CreatedBy = createdBy,
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

        // ==========================================
        // 3. TRANSFER DOCUMENT (Chuyển kho - Type 3)
        // ==========================================
        // BƯỚC 1: TẠO PHIẾU CHUYỂN KHO (TẠI KHO XUẤT - BẬT TRẠNG THÁI PENDING)
        public async Task<Document> CreateTransferDocumentAsync(TransferDocumentCreateDto dto, long? createdBy = null)
        {
            var fromBranch = await _repo.GetBranchAsync(dto.FromBranchId);
            var toBranch = await _repo.GetBranchAsync(dto.ToBranchId);

            if (fromBranch == null || toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound, "Không tìm thấy chi nhánh chuyển hoặc nhận.");

            if (fromBranch.PostingLockDate.HasValue && dto.BusinessDate.Date <= fromBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày chứng từ nằm trước ngày khóa sổ của chi nhánh xuất.");

            var fromBinvIds = dto.Details.Select(d => d.FromBInventoryId).Distinct().ToList();
            var fromBinvs = await _repo.GetBInventoriesByIdsAsync(fromBinvIds);

            foreach (var binv in fromBinvs)
            {
                if (binv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể chuyển kho sản phẩm [{binv.Product.Name}] do thuộc loại Processed.");
            }

            var now = DateTime.UtcNow;
            string code = $"TR{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var rawDate = dto.BusinessDate.ToUniversalTime();
            var exactSecondDate = new DateTime(rawDate.Year, rawDate.Month, rawDate.Day, rawDate.Hour, rawDate.Minute, rawDate.Second, DateTimeKind.Utc);

            var document = new Document
            {
                BranchId = dto.FromBranchId,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.Pending, // Treo hàng chờ kho nhận bấm nhận
                TotalAmount = 0,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(fromBinvIds, document.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(fromBinvIds, document.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();
            decimal totalTransferValue = 0;

            foreach (var detail in dto.Details)
            {
                var fromBinv = fromBinvs.FirstOrDefault(b => b.Id == detail.FromBInventoryId);
                if (fromBinv == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Sản phẩm chuyển kho không tồn tại tại kho xuất.");

                var priorFromLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == fromBinv.Id);
                decimal fromQty = priorFromLedger?.RunningQuantity ?? 0;
                decimal fromAvg = priorFromLedger?.RunningAverageCost ?? 0;
                decimal fromLeftOver = priorFromLedger?.RunningLeftOver ?? 0;

                // RÀNG BUỘC: Chi nhánh xuất phải có đủ tồn kho tại mốc thời gian chuyển
                if (fromQty < detail.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Không thể chuyển kho sản phẩm [{fromBinv.Product.Name}]. Số lượng chuyển ({detail.Quantity}) lớn hơn số tồn kho tại chi nhánh xuất tại mốc thời gian ({fromQty}).");
                }

                decimal fromVal = (fromQty * fromAvg) + fromLeftOver;

                var fromDocDetail = new DocumentDetail
                {
                    BInventoryId = fromBinv.Id,
                    Quantity = detail.Quantity,
                    SystemQuantity = detail.Quantity,
                    UnitPrice = fromAvg,
                    Note = $"Xuất chuyển sang kho {toBranch.Name}: {detail.Note}",
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(fromDocDetail);

                var fromLedger = new InventoryLedger
                {
                    BInventoryId = fromBinv.Id,
                    BusinessDate = document.BusinessDate,
                    DocumentType = document.Type,
                    QuantityDelta = -detail.Quantity,
                    InventoryValueDelta = 0,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = fromAvg,
                    DocumentDetail = fromDocDetail,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(fromLedger, fromQty, fromVal, fromAvg, fromLeftOver);
                decimal transferredValue = Math.Abs(fromLedger.InventoryValueDelta);
                totalTransferValue += transferredValue;

                fromQty = fromLedger.RunningQuantity;
                fromVal = fromLedger.RunningInventoryValue;
                fromAvg = fromLedger.RunningAverageCost;
                fromLeftOver = fromLedger.RunningLeftOver;
                ledgersToCreate.Add(fromLedger);

                if (afterLedgersDict.TryGetValue(fromBinv.Id, out var subsequentFromLedgers))
                {
                    foreach (var ledger in subsequentFromLedgers)
                    {
                        RecalculateLedgerState(ledger, fromQty, fromVal, fromAvg, fromLeftOver);
                        fromQty = ledger.RunningQuantity;
                        fromVal = ledger.RunningInventoryValue;
                        fromAvg = ledger.RunningAverageCost;
                        fromLeftOver = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                fromBinv.Quantity = fromQty;
                fromBinv.Avg = fromAvg;
                fromBinv.LeftOver = fromLeftOver;
                updatedBInvs.Add(fromBinv);
            }

            document.TotalAmount = totalTransferValue;

            return await _repo.ExecuteDocumentTransactionAsync(
                document, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        // BƯỚC 2: XÁC NHẬN NHẬN HÀNG (TẠI KHO NHẬN - THAM CHIẾU FATHER ID PRO)
        public async Task<Document> ReceiveTransferDocumentAsync(TransferReceiveDocumentCreateDto dto, long? createdBy = null)
        {
            var parentTransferDoc = await _repo.GetDocumentWithDetailsAsync(dto.TransferDocumentId);
            if (parentTransferDoc == null || parentTransferDoc.IsDeleted)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Không tìm thấy phiếu chuyển kho gốc hoặc phiếu đã bị xóa.");

            if (parentTransferDoc.Type != DocumentType.Transfer)
                throw new MenuGoException(ErrorCodes.DocumentNotFound, "Chứng từ tham chiếu không phải phiếu chuyển kho.");

            if (parentTransferDoc.TransferStatus == DocumentTransferStatus.Received)
                throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, "Phiếu chuyển kho này đã được nhận trước đó.");

            var rawReceiveDate = dto.ReceivedDate.ToUniversalTime();
            var exactReceiveDate = new DateTime(rawReceiveDate.Year, rawReceiveDate.Month, rawReceiveDate.Day, rawReceiveDate.Hour, rawReceiveDate.Minute, rawReceiveDate.Second, DateTimeKind.Utc);

            if (exactReceiveDate.Date < parentTransferDoc.BusinessDate.Date)
                throw new MenuGoException(ErrorCodes.DocumentInvalidReturnBusinessDate, "Ngày nhận hàng không được nhỏ hơn ngày xuất chuyển từ kho gửi.");

            var toBinvIds = dto.Details.Select(d => d.ToBInventoryId).Distinct().ToList();
            var toBinvs = await _repo.GetBInventoriesByIdsAsync(toBinvIds);

            if (toBinvs.Count != toBinvIds.Count)
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, "Một hoặc nhiều sản phẩm nhận kho không tồn tại.");

            var toBranchId = toBinvs.First().BranchId;
            var toBranch = await _repo.GetBranchAsync(toBranchId);

            if (toBranch == null)
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);

            if (toBranch.PostingLockDate.HasValue && exactReceiveDate.Date <= toBranch.PostingLockDate.Value.Date)
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, "Ngày nhận hàng nằm trước hoặc bằng ngày khóa sổ của chi nhánh nhận.");

            // Kiểm tra số lượng nhận không vượt quá số lượng gửi
            foreach (var detail in dto.Details)
            {
                var toBinv = toBinvs.First(b => b.Id == detail.ToBInventoryId);
                if (toBinv.Product.Type == ProductType.Processed)
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Không thể nhận kho sản phẩm [{toBinv.Product.Name}] do thuộc loại Processed.");

                var parentDetail = parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.BInventory.ProductId == toBinv.ProductId);
                if (parentDetail == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Sản phẩm [{toBinv.Product.Name}] không nằm trong danh sách hàng gửi.");

                if (detail.ReceivedQuantity > parentDetail.Quantity)
                {
                    throw new MenuGoException(
                        ErrorCodes.DocumentReturnQuantityExceeded, 
                        $"Số lượng thực nhận ({detail.ReceivedQuantity}) không được lớn hơn số lượng xuất gửi từ chi nhánh gốc ({parentDetail.Quantity}).");
                }
            }

            var now = DateTime.UtcNow;
            string code = $"RC{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            var receiveDocument = new Document
            {
                BranchId = toBranchId,
                BusinessDate = exactReceiveDate,
                Code = code,
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.Received,
                TotalAmount = parentTransferDoc.TotalAmount,
                ParentDocumentId = parentTransferDoc.Id, // Father ID
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            // Cập nhật phiếu gốc sang Received
            parentTransferDoc.TransferStatus = DocumentTransferStatus.Received;

            var beforeLedgers = await _repo.GetLatestLedgersBeforeOrEqualDateAsync(toBinvIds, receiveDocument.BusinessDate);
            var afterLedgers = await _repo.GetLedgersAfterDateAsync(toBinvIds, receiveDocument.BusinessDate);
            var afterLedgersDict = afterLedgers.GroupBy(l => l.BInventoryId).ToDictionary(g => g.Key, g => g.ToList());

            var details = new List<DocumentDetail>();
            var ledgersToCreate = new List<InventoryLedger>();
            var ledgersToUpdate = new List<InventoryLedger>();
            var updatedBInvs = new List<BInventory>();

            foreach (var detail in dto.Details)
            {
                var toBinv = toBinvs.First(b => b.Id == detail.ToBInventoryId);
                var parentDetail = parentTransferDoc.DocumentDetails.FirstOrDefault(d => d.BInventory.ProductId == toBinv.ProductId);
                
                decimal transferredValue = parentDetail != null ? parentDetail.Quantity * parentDetail.UnitPrice : 0;
                decimal unitCost = detail.ReceivedQuantity > 0 ? transferredValue / detail.ReceivedQuantity : 0;
                unitCost = Math.Truncate(unitCost * 1000000m) / 1000000m;

                var docDetail = new DocumentDetail
                {
                    BInventoryId = toBinv.Id,
                    Quantity = detail.ReceivedQuantity,
                    SystemQuantity = detail.ReceivedQuantity,
                    UnitPrice = unitCost,
                    Note = detail.Note ?? $"Nhận chuyển kho từ phiếu {parentTransferDoc.Code}",
                    CreatedAt = DateTime.UtcNow
                };
                details.Add(docDetail);

                var priorToLedger = beforeLedgers.FirstOrDefault(l => l.BInventoryId == toBinv.Id);
                decimal toQty = priorToLedger?.RunningQuantity ?? 0;
                decimal toAvg = priorToLedger?.RunningAverageCost ?? 0;
                decimal toLeftOver = priorToLedger?.RunningLeftOver ?? 0;
                decimal toVal = (toQty * toAvg) + toLeftOver;

                var toLedger = new InventoryLedger
                {
                    BInventoryId = toBinv.Id,
                    BusinessDate = receiveDocument.BusinessDate,
                    DocumentType = receiveDocument.Type,
                    QuantityDelta = detail.ReceivedQuantity,
                    InventoryValueDelta = transferredValue,
                    RunningQuantity = 0,
                    RunningInventoryValue = 0,
                    RunningAverageCost = 0,
                    RunningLeftOver = 0,
                    UnitCost = unitCost,
                    DocumentDetail = docDetail,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                RecalculateLedgerState(toLedger, toQty, toVal, toAvg, toLeftOver);
                toQty = toLedger.RunningQuantity;
                toVal = toLedger.RunningInventoryValue;
                toAvg = toLedger.RunningAverageCost;
                toLeftOver = toLedger.RunningLeftOver;
                ledgersToCreate.Add(toLedger);

                if (afterLedgersDict.TryGetValue(toBinv.Id, out var subsequentToLedgers))
                {
                    foreach (var ledger in subsequentToLedgers)
                    {
                        RecalculateLedgerState(ledger, toQty, toVal, toAvg, toLeftOver);
                        toQty = ledger.RunningQuantity;
                        toVal = ledger.RunningInventoryValue;
                        toAvg = ledger.RunningAverageCost;
                        toLeftOver = ledger.RunningLeftOver;
                        ledgersToUpdate.Add(ledger);
                    }
                }

                toBinv.Quantity = toQty;
                toBinv.Avg = toAvg;
                toBinv.LeftOver = toLeftOver;
                updatedBInvs.Add(toBinv);
            }

            return await _repo.ExecuteDocumentTransactionAsync(
                receiveDocument, details, new List<DocumentPartner>(), updatedBInvs,
                ledgersToCreate, ledgersToUpdate, new List<CashFlow>(), new List<CashFlowDetail>());
        }

        // HỦY PHIẾU CHUYỂN KHO: KIỂM TRA KHÓA SỔ NGHUYÊN NGẶC CẢ 2 CHI NHÁNH
        public async Task DeleteTransferDocumentAsync(long documentId, long? deletedBy = null)
        {
            var doc = await _repo.GetDocumentWithDetailsAsync(documentId);
            if (doc == null || doc.IsDeleted)
                return;

            if (doc.Branch.PostingLockDate.HasValue && doc.BusinessDate.Date <= doc.Branch.PostingLockDate.Value.Date)
            {
                throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, $"Không thể xóa phiếu chuyển kho do ngày chứng từ nằm trước hoặc bằng ngày khóa sổ của chi nhánh {doc.Branch.Name}.");
            }

            // Nếu đây là phiếu chuyển kho gốc (Parent)
            if (doc.ParentDocumentId == null)
            {
                var childReceiveDoc = await _repo.GetChildReceiveDocumentAsync(doc.Id);
                if (childReceiveDoc != null && !childReceiveDoc.IsDeleted)
                {
                    if (childReceiveDoc.Branch.PostingLockDate.HasValue && childReceiveDoc.BusinessDate.Date <= childReceiveDoc.Branch.PostingLockDate.Value.Date)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentPostingLockDateError, $"Không thể xóa do chi nhánh nhận {childReceiveDoc.Branch.Name} đã khóa sổ.");
                    }
                    await DeleteDocumentAsync(childReceiveDoc.Id, deletedBy);
                }
            }

            await DeleteDocumentAsync(documentId, deletedBy);
        }

        // ==========================================
        // 4. RETURN DOCUMENT (Trả hàng nhập - Type 4)
        // ==========================================
        public async Task<Document> CreateReturnDocumentAsync(ReturnDocumentCreateDto dto, long? createdBy = null)
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

            var document = new Document
            {
                BranchId = branch.Id,
                BusinessDate = exactSecondDate,
                Code = code,
                Type = DocumentType.Return,
                Status = DocumentStatus.Completed,
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

        // ==========================================
        // 5. CHECK DOCUMENT (Kiểm kho - Type 5)
        // ==========================================
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

        // ==========================================
        // 6. PRODUCTION DOCUMENT (Sản xuất - Type 6)
        // ==========================================
        public async Task<Document> CreateProductionDocumentAsync(ProductionDocumentCreateDto dto, long? createdBy = null)
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

            var document = new Document
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
            finishedUnitCost = Math.Truncate(finishedUnitCost * 1000000m) / 1000000m;

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

        // ==========================================
        // GENERIC DELETE DOCUMENT HANDLER
        // ==========================================
        public async Task DeleteDocumentAsync(long documentId, long? deletedBy = null)
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

        // ==========================================
        // UNIFIED RECALCULATE LEDGER STATE ENGINE
        // ==========================================
        private void RecalculateLedgerState(
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
                        newAvgCost = incomingUnitCost;
                    }
                    else if (ledger.DocumentType == DocumentType.Check)
                    {
                        newTotalValue = valueDelta;
                        newAvgCost = nextQty > 0 ? (newTotalValue / nextQty) : previousAvgCost;
                    }
                }
                else
                {
                    newAvgCost = Math.Truncate((newTotalValue / nextQty) * 1000000m) / 1000000m;
                }

                newLeftOver = newTotalValue - (nextQty * newAvgCost);
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
