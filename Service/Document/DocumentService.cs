using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Service.Document
{
    /// <summary>
    /// Service triển khai logic nghiệp vụ riêng biệt cho 9 loại chứng từ, tuân thủ nghiêm ngặt quy tắc vòng đời trạng thái.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        #region Khai báo Repository & Constructor

        #region Repository, Mapper & FEFO Service Dependency
        /// <summary>
        /// Instance DocumentRepository thao tác với DB.
        /// </summary>
        private readonly IDocumentRepository _repo;
        private readonly IMapper _mapper;
        private readonly IFefoAllocationService? _fefoService;
        #endregion

        #region Constructor
        /// <summary>
        /// Khởi tạo DocumentService với IDocumentRepository, IMapper và IFefoAllocationService.
        /// </summary>
        /// <param name="repo">Repository chứng từ</param>
        /// <param name="mapper">AutoMapper instance</param>
        /// <param name="fefoService">Dịch vụ phân bổ FEFO</param>
        public DocumentService(IDocumentRepository repo, IMapper mapper, IFefoAllocationService? fefoService = null)
        {
            _repo = repo;
            _mapper = mapper;
            _fefoService = fefoService;
        }
        #endregion

        #endregion

        #region Quy tắc Kiểm tra & Khóa chứng từ chung (Common Validation Rules Implementation)

        #region Hàm Kiểm tra bảo vệ phiếu đã khóa (Completed / Canceled Guard)
        /// <summary>
        /// Khóa tuyệt đối: Nếu chứng từ đã ở trạng thái Completed hoặc Canceled, nghiêm cấm mọi hành vi sửa/xóa.
        /// </summary>
        public void ValidateDocumentNotLocked(Models.Document document)
        {
            if (document.Status == DocumentStatus.Completed)
            {
                throw new MenuGoException(ErrorCodes.DocumentLockedCompleted, $"Chứng từ {document.Code} đã CHỐT (Completed). Không được phép chỉnh sửa hoặc xóa.");
            }

            if (document.Status == DocumentStatus.Cancelled)
            {
                throw new MenuGoException(ErrorCodes.DocumentLockedCancelled, $"Chứng từ {document.Code} đã bị HỦY (Canceled). Không được phép thao tác.");
            }
        }
        #endregion

        #region Hàm Kiểm tra phụ thuộc chứng từ gốc (Parent Document Validation)
        /// <summary>
        /// Kiểm tra ràng buộc phiếu Return phải gắn với Import, phiếu CustomerReturn phải gắn với Sale.
        /// </summary>
        public async Task ValidateParentDependencyRuleAsync(DocumentType type, long? parentDocumentId)
        {
            #region Ràng buộc Return -> Import
            if (type == DocumentType.Return)
            {
                if (!parentDocumentId.HasValue)
                {
                    throw new MenuGoException(ErrorCodes.DocumentParentRequired, "Phiếu Trả Hàng (Return) BẮT BUỘC phải trỏ tới chứng từ Nhập kho (Import) gốc.");
                }

                bool isValidParent = await _repo.ValidateParentDocumentAsync(parentDocumentId.Value, DocumentType.Import);
                if (!isValidParent)
                {
                    throw new MenuGoException(ErrorCodes.DocumentParentNotCompleted, $"Chứng từ gốc ID {parentDocumentId.Value} không hợp lệ hoặc chưa ở trạng thái Completed.");
                }
            }
            #endregion

            #region Ràng buộc CustomerReturn -> Sale
            if (type == DocumentType.CustomerReturn)
            {
                if (!parentDocumentId.HasValue)
                {
                    throw new MenuGoException(ErrorCodes.DocumentParentRequired, "Phiếu Khách Trả Hàng (CustomerReturn) BẮT BUỘC phải trỏ tới chứng từ Xuất bán (Sale) gốc.");
                }

                bool isValidParent = await _repo.ValidateParentDocumentAsync(parentDocumentId.Value, DocumentType.Sale);
                if (!isValidParent)
                {
                    throw new MenuGoException(ErrorCodes.DocumentParentNotCompleted, $"Chứng từ bán gốc ID {parentDocumentId.Value} không hợp lệ hoặc chưa ở trạng thái Completed.");
                }
            }
            #endregion
        }
        #endregion

        #region Lấy DTO phản hồi dùng chung (Generic GET Response DTOs Implementation)
        /// <summary>
        /// Lấy chi tiết chứng từ bất kỳ dưới dạng DocumentResponseDto (Phản hồi dùng chung).
        /// </summary>
        public async Task<DocumentResponseDto?> GetDocumentByIdDtoAsync(long id)
        {
            var document = await _repo.GetByIdAsync(id);
            if (document == null) return null;

            return _mapper.Map<DocumentResponseDto>(document);
        }

        /// <summary>
        /// Lấy danh sách chứng từ dưới dạng DocumentListResponseDto (Phản hồi dùng chung dạng bảng).
        /// </summary>
        public async Task<IEnumerable<DocumentListResponseDto>> GetDocumentListDtosAsync(long branchId, DocumentType? type = null, DocumentStatus? status = null)
        {
            IEnumerable<Models.Document> documents;
            if (type == DocumentType.Transfer)
            {
                documents = await _repo.GetTransferDocumentsAsync(branchId);
                if (status.HasValue)
                {
                    documents = documents.Where(d => d.Status == status.Value);
                }
            }
            else
            {
                documents = await _repo.GetDocumentsAsync(branchId, type, status);
            }

            return _mapper.Map<IEnumerable<DocumentListResponseDto>>(documents);
        }
        #endregion

        #region Helper chung để tạo phiếu Pending
        /// <summary>
        /// Helper nội bộ thiết lập thuộc tính khởi tạo cho phiếu Pending.
        /// </summary>
        private async Task<Models.Document> CreatePendingBaseAsync(Models.Document document, DocumentType targetType, long userId)
        {
            document.Type = targetType;
            document.Status = DocumentStatus.Pending;
            document.CreatedBy = userId;
            document.CreatedAt = DateTime.UtcNow;

            // Xóa rỗng các Snapshot khi ở trạng thái Pending
            document.SnapshotBranchName = string.Empty;
            document.SnapshotPartnerName = null;
            document.SnapshotCreatedByName = null;
            document.SnapshotCreatedByUsername = null;

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion

        #region Helper chung để cập nhật phiếu Pending
        /// <summary>
        /// Helper nội bộ cập nhật thông tin phiếu Pending.
        /// </summary>
        private async Task<Models.Document> UpdatePendingBaseAsync(long documentId, Models.Document updatedDocument, DocumentType expectedType)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != expectedType)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ {expectedType} với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chỉnh sửa khi chứng từ ở trạng thái Pending.");
            }

            existing.Note = updatedDocument.Note;
            existing.OrderDate = updatedDocument.OrderDate;
            existing.TotalAmount = updatedDocument.TotalAmount;
            existing.AmountDue = updatedDocument.AmountDue;
            existing.AmountPaid = updatedDocument.AmountPaid;
            existing.ImageUrls = updatedDocument.ImageUrls;

            if (updatedDocument.DocumentDetails != null && !ReferenceEquals(existing.DocumentDetails, updatedDocument.DocumentDetails))
            {
                if (existing.DocumentDetails != null && existing.DocumentDetails.Any())
                {
                    _repo.RemoveDetails(existing.DocumentDetails.ToList());
                    existing.DocumentDetails.Clear();
                }
                else if (existing.DocumentDetails == null)
                {
                    existing.DocumentDetails = new List<Models.DocumentDetail>();
                }

                foreach (var detail in updatedDocument.DocumentDetails)
                {
                    existing.DocumentDetails.Add(detail);
                }
            }

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }
        #endregion

        #region Helper chung để Chốt phiếu Completed
        /// <summary>
        /// Helper nội bộ chốt phiếu chuyển trạng thái Pending -> Completed.
        /// </summary>
        private async Task<Models.Document> CompleteBaseAsync(long documentId, DocumentType expectedType, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != expectedType)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ {expectedType} với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            // TODO: Đổ bê tông Master Data Snapshots & Ghi Sổ kho InventoryLedger

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }
        #endregion

        #region Helper chung để Xóa mềm phiếu Pending
        /// <summary>
        /// Helper nội bộ thực hiện xóa mềm phiếu Pending (chuyển sang Canceled).
        /// Đổ bê tông Master Data Snapshot tại thời điểm xóa mềm.
        /// </summary>
        private async Task<bool> SoftDeletePendingBaseAsync(long documentId, DocumentType expectedType, long userId, string deleteNote)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != expectedType)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ {expectedType} với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            #region Quy tắc: DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(deleteNote) && deleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            _repo.SoftDeletePending(existing, userId, deleteNote);

            // Quy tắc: Khi xóa 1 phiếu Pending -> Đổ bê tông lưu Snapshot Master Data
            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: true);

            return await _repo.SaveChangesAsync();
        }
        #endregion

        #endregion

        #region 1. Nghiệp vụ Chứng từ Nhập kho (Import)

        #region Validation Nghiệp Vụ Phiếu Nhập Kho (Import Validation Rules)

        #region Kiểm tra quy tắc chung cho Phiếu Nhập (Lưu tạm & Chốt)
        /// <summary>
        /// Kiểm tra các quy tắc bắt buộc chung (số học, chi tiết, comment, trùng lặp) cho phiếu nhập kho.
        /// </summary>
        private async Task ValidateImportBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ nhập kho không được để trống.");
            }

            #region Quy tắc: Comment/Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Tối thiểu phải có 1 DocumentDetail
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu nhập kho bắt buộc phải có tối thiểu 1 mặt hàng chi tiết (DocumentDetail).");
            }
            #endregion

            #region Quy tắc a5: Mỗi BInventoryId chỉ xuất hiện 1 lần (Không trùng lặp mặt hàng trong phiếu)
            var duplicateBInventory = document.DocumentDetails
                .GroupBy(d => d.BInventoryId)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateBInventory != null)
            {
                throw new MenuGoException(ErrorCodes.DocumentDuplicateDetail, $"Mặt hàng BInventory ID {duplicateBInventory.Key} bị trùng lặp trong phiếu nhập. Mỗi mặt hàng chỉ được xuất hiện 1 lần.");
            }
            #endregion

            #region Quy tắc a2, a4 & Loại sản phẩm hợp lệ: Kiểm tra số lượng (0.001 <= Quantity <= 10 tỷ), ProductType và UnitConversion
            const decimal MIN_QTY = 0.001m;
            const decimal MAX_QTY = 10_000_000_000m;
            const decimal MAX_AMOUNT = 10_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy mặt hàng BInventory ID: {detail.BInventoryId}");
                }

                #region Kiểm tra loại sản phẩm chỉ được phép nhập (Tool, Regular, Ingredient)
                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                var productType = binventory.Product.Type;
                if (productType != ProductType.Tool && productType != ProductType.Regular && productType != ProductType.Ingredient)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInvalidProductTypeForImport, $"Sản phẩm '{binventory.Product.Name}' có loại (ProductType = {productType}) không được phép nhập kho. Chỉ cho phép các loại: Tool, Regular, Ingredient.");
                }
                #endregion

                if (detail.Quantity < MIN_QTY || detail.Quantity > MAX_QTY)
                {
                    throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng nhập của mặt hàng '{binventory.Product.Name}' ({detail.Quantity}) không hợp lệ. Phải nằm trong khoảng từ 0.001 đến 10 tỷ.");
                }

                #region Kiểm tra đơn giá (UnitPrice không âm và không vượt quá 10 tỷ)
                const decimal MAX_UNIT_PRICE = 10_000_000_000m;
                if (detail.UnitPrice < 0 || detail.UnitPrice > MAX_UNIT_PRICE)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice, $"Đơn giá nhập của mặt hàng '{binventory.Product.Name}' ({detail.UnitPrice}) không hợp lệ. Đơn giá phải từ 0 đến 10 tỷ VNĐ.");
                }
                #endregion

                #region Quy tắc a4: Đơn vị tính và kiểm tra thuộc tính sở hữu UnitConversion
                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion == null || unitConversion.ProductId != binventory.ProductId)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Đơn vị quy đổi ID {detail.UnitConversionId.Value} không tồn tại hoặc không thuộc về sản phẩm '{binventory.Product.Name}'.");
                    }

                    if (unitConversion.ConversionPoint <= 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Tỷ lệ quy đổi của đơn vị tính ID {detail.UnitConversionId.Value} phải lớn hơn 0.");
                    }

                    detail.ConversionRate = unitConversion.ConversionPoint;
                }
                else
                {
                    detail.ConversionRate = 1;
                }
                #endregion

                // Bỏ qua BaseQuantity do client truyền vào -> Tự động tính toán số lượng kho cơ sở chuẩn:
                // BaseQuantity = Quantity * (ConversionPoint nếu chọn, hoặc 1 nếu không)
                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
            }
            #endregion

            #region Phân định rõ Tiền đã trả (AmountPaid) & Tiền phải trả (AmountDue) tính toán tại DB
            // 1. Tự động tính lại TotalAmount = sum(Quantity * UnitPrice)
            decimal calculatedTotalAmount = document.DocumentDetails.Sum(d => d.Quantity * d.UnitPrice);
            if (calculatedTotalAmount < 0 || calculatedTotalAmount > MAX_AMOUNT)
            {
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit, $"Tổng giá trị phiếu nhập kho ({calculatedTotalAmount:N0} VNĐ) vượt quá 10 tỷ VNĐ.");
            }
            document.TotalAmount = calculatedTotalAmount;

            var (roundedTotal, roundingValue) = CalculateDocumentRounding(calculatedTotalAmount);

            // Trích xuất số tiền trừ nợ NCC từ Note nếu có
            decimal debtDeduction = ExtractDebtDeduction(document.Note);
            decimal effectivePayable = Math.Max(0, roundedTotal - debtDeduction);

            // 2. Validate AmountPaid đối với giá trị phải trả đã làm tròn (sau khi trừ nợ NCC nếu có)
            if (document.AmountPaid < 0 || document.AmountPaid > roundedTotal)
            {
                throw new MenuGoException(ErrorCodes.DocumentAmountPaidInvalid, $"Số tiền đã trả (AmountPaid: {document.AmountPaid:N0} VNĐ) không hợp lệ. Phải nằm trong khoảng từ 0 đến Tổng tiền chứng từ sau làm tròn ({roundedTotal:N0} VNĐ).");
            }

            // 3. Tự động tính toán số tiền còn nợ/phải trả tại backend/DB: AmountDue = effectivePayable - AmountPaid
            document.AmountDue = Math.Max(0, effectivePayable - document.AmountPaid);

            // 4. Quy tắc: Nếu phát sinh tiền đã trả (AmountPaid > 0) -> Ngay cả phiếu Pending cũng BẮT BUỘC chỉ định Nhà cung cấp (PartnerId)
            if (document.AmountPaid > 0 && (!document.PartnerId.HasValue || document.PartnerId.Value <= 0))
            {
                throw new MenuGoException(ErrorCodes.DocumentSupplierRequiredForPayment, "Khi phiếu nhập tạm phát sinh tiền đã trả (AmountPaid > 0), BẮT BUỘC phải chọn Nhà cung cấp (Supplier).");
            }
            #endregion
        }
        #endregion

        #region Kiểm tra quy tắc bắt buộc khi Chốt kho (Completed)
        /// <summary>
        /// Kiểm tra điều kiện bắt buộc khi Chốt phiếu Nhập kho.
        /// </summary>
        private async Task ValidateImportCompletedRulesAsync(Models.Document document)
        {
            await ValidateImportBaseRulesAsync(document);

            #region Quy tắc a3: Bắt buộc phải chọn Supplier (PartnerId) cho phiếu chốt
            if (!document.PartnerId.HasValue || document.PartnerId.Value <= 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentSupplierRequiredForComplete, "Khi chốt phiếu Nhập kho (Completed), BẮT BUỘC phải chọn Nhà cung cấp (Supplier).");
            }
            #endregion
        }
        #endregion

        #region Phát sinh Mã Chứng Từ NH (Rule a6)
        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Nhập kho dựa trên ID.
        /// </summary>
        private async Task GenerateOrValidateImportCodeAsync(Models.Document document)
        {
            #region Quy tắc a6: Nếu user tự nhập -> check trùng. Nếu trống -> gen code NH + Id 6 chữ số
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"NH{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"NH{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
            #endregion
        }
        #endregion

        #endregion

        #region Thuật toán Tính Giá Trung Bình (Avg Cost) & Ghi Sổ Kho (Inventory Ledger)

        /// <summary>
        /// Thực thi thuật toán tính giá vốn trung bình (Weighted Average Cost) cho từng mặt hàng khi chốt nhập kho:
        /// b1 = (q_old * avg_old) + leftover_old + (q_import * unit_price)
        /// b2 = q_old + q_import
        /// avg = Math.Round(b1 / b2, 6)
        /// c1 = avg * b2
        /// leftover = b1 - c1
        /// </summary>
        private async Task ProcessWeightedAvgCostAndLedgerAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng BInventory ID: {detail.BInventoryId}");
                }

                // 1. Lấy tồn kho hiện tại (q_old), giá vốn hiện tại (avg_old) và leftover cũ (leftover_old)
                decimal q_old = binventory.Quantity;
                decimal avg_old = binventory.Avg;
                decimal leftover_old = binventory.LeftOver;

                // 2. Xác định đơn giá nhập theo đơn vị kho cơ sở
                decimal unit_cost_import = (detail.ConversionRate > 0) ? (detail.UnitPrice / detail.ConversionRate) : detail.UnitPrice;
                decimal val_import = detail.Quantity * detail.UnitPrice;
                decimal q_import = detail.BaseQuantity;

                decimal avg;
                decimal leftover;
                decimal b2;

                if (q_old <= 0)
                {
                    // Tồn kho trước khi nhập <= 0 (về 0 hoặc âm):
                    // Giá vốn (Avg) được làm mới hoàn toàn dựa trên đơn giá lô nhập mới này, triệt tiêu LeftOver cũ.
                    avg = Math.Round(unit_cost_import, 6);
                    leftover = 0m;
                    b2 = q_import;
                    binventory.Quantity = b2;
                }
                else
                {
                    // Tồn kho trước khi nhập > 0: Áp dụng công thức tính giá vốn bình quân gia quyền (Weighted Average Cost)
                    decimal b1 = (q_old * avg_old) + leftover_old + val_import;
                    b2 = q_old + q_import;
                    avg = (b2 > 0) ? Math.Round(b1 / b2, 6) : 0m;
                    decimal c1 = avg * b2;
                    leftover = b1 - c1;
                    binventory.Quantity = b2;
                }

                // 7. Cập nhật lại BInventory
                binventory.Avg = avg;
                binventory.LeftOver = leftover;
                
                // Tự động bật lại cảnh báo tồn kho nếu đang tắt (do nhập thêm hàng)
                if (!binventory.IsAlertEnabled)
                {
                    binventory.IsAlertEnabled = true;
                }

                // Snapshot Avg Cost vào chi tiết chứng từ
                detail.SnapshotAvgCost = avg;

                // 8. Tạo bản ghi Sổ kho InventoryLedger
                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Import,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = !string.IsNullOrWhiteSpace(detail.SnapshotProductName) ? detail.SnapshotProductName : (binventory.Product?.Name ?? string.Empty),
                    SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventory.Product?.Name ?? string.Empty),
                    ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                    QuantityDelta = q_import,
                    InventoryValueDelta = val_import,
                    RunningQuantity = b2,
                    RunningInventoryValue = b2 == 0 ? 0m : (b2 * avg + leftover),
                    RunningAverageCost = avg,
                    UnitCost = unit_cost_import,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);

                // 8b. Xử lý Lô hàng (BInventoryBatch) khi nhập kho
                string batchCode = !string.IsNullOrWhiteSpace(detail.BatchCodeSnapshot)
                    ? detail.BatchCodeSnapshot
                    : $"LOT-IMP-{document.Code}-{detail.Id}";

                detail.BatchCodeSnapshot = batchCode;

                var existingBatch = await _repo.GetBatchByInventoryAndCodeAsync(binventory.Id, batchCode);
                if (existingBatch != null)
                {
                    // Nếu Lô đã tồn tại, cộng dồn số lượng
                    existingBatch.QuantityOriginal += q_import;
                    existingBatch.QuantityRemaining += q_import;
                    if (existingBatch.Status == BatchStatus.Depleted)
                    {
                        existingBatch.Status = BatchStatus.Active;
                    }
                    if (detail.ManufactureDateSnapshot.HasValue)
                    {
                        existingBatch.ManufactureDate = detail.ManufactureDateSnapshot;
                    }
                    if (detail.ExpiryDateSnapshot.HasValue)
                    {
                        existingBatch.ExpiryDate = detail.ExpiryDateSnapshot;
                    }
                    existingBatch.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Tạo Lô mới
                    var newBatch = new BInventoryBatch
                    {
                        BInventoryId = binventory.Id,
                        BatchCode = batchCode,
                        QuantityOriginal = q_import,
                        QuantityRemaining = q_import,
                        UnitCost = unit_cost_import,
                        ManufactureDate = detail.ManufactureDateSnapshot,
                        ExpiryDate = detail.ExpiryDateSnapshot,
                        ReceivedDate = document.PostedAt ?? DateTime.UtcNow,
                        Status = BatchStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repo.AddBatchAsync(newBatch);
                }
            }
        }
        #endregion

        #region Xử lý Phát Sinh Phiếu CashFlow Khi Thanh Toán (Rule a7 & Rounding)

        /// <summary>
        /// Tính toán làm tròn giá tiền theo quy tắc:
        /// - phần dư < 0.5 thì làm tròn về hàng đơn vị (tròn xuống)
        /// - phần dư >= 0.5 thì làm tròn lên hàng đơn vị
        /// RoundingValue = RawTotal - RoundedTotal (có thể dương, âm hoặc 0).
        /// EffectivePayable = RawTotal - RoundingValue = RoundedTotal.
        /// </summary>
        public static (decimal roundedTotal, decimal roundingValue) CalculateDocumentRounding(decimal rawTotal)
        {
            decimal remainder = rawTotal - Math.Floor(rawTotal);
            decimal roundedTotal = remainder < 0.5m ? Math.Floor(rawTotal) : Math.Floor(rawTotal) + 1m;
            decimal roundingValue = rawTotal - roundedTotal;
            return (roundedTotal, roundingValue);
        }

        #region Helper trích xuất số tiền trừ nợ NCC từ Note
        /// <summary>
        /// Trích xuất số tiền đã trừ nợ NCC từ ghi chú phiếu nhập kho (ví dụ: 'Trừ tiền NCC nợ: 25.000 đ').
        /// </summary>
        public static decimal ExtractDebtDeduction(string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) return 0m;
            var match = System.Text.RegularExpressions.Regex.Match(note, @"Trừ tiền NCC nợ:\s*([0-9.,]+)");
            if (match.Success)
            {
                var rawNum = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(rawNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
                {
                    return val;
                }
            }
            return 0m;
        }
        #endregion

        /// <summary>
        /// Tạo phiếu chi CashFlow thanh toán cho Nhà cung cấp nếu AmountPaid > 0 khi chốt phiếu,
        /// và tự động sinh phiếu CashFlow Rounding nếu có chênh lệch làm tròn.
        /// </summary>
        private async Task CreateImportCashFlowAsync(Models.Document document)
        {
            var (roundedTotal, roundingValue) = CalculateDocumentRounding(document.TotalAmount);

            Console.WriteLine($"[DEBUG CashFlow] DocumentId={document.Id}, Code={document.Code}, AmountPaid={document.AmountPaid}, TotalAmount={document.TotalAmount}");

            #region Quy tắc a7: Nếu AmountPaid > 0 khi chốt phiếu -> Tạo CashFlow Thanh Toán
            if (document.AmountPaid > 0)
            {
                Console.WriteLine($"[DEBUG CashFlow] Creating Outflow for document {document.Code}, amount={document.AmountPaid}");
                var cashFlow = new CashFlow
                {
                    BranchId = document.BranchId,
                    DocumentId = document.Id,
                    PartnerId = document.PartnerId,
                    Code = $"CF_{document.Code}",
                    BusinessDate = DateTime.UtcNow,
                    Direction = CashFlowDirection.Outflow,
                    Type = CashFlowDetailType.Payment,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = document.AmountPaid,
                    Note = $"Thanh toán cho nhà cung cấp phiếu nhập kho {document.Code}",
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddCashFlowAsync(cashFlow);
                Console.WriteLine($"[DEBUG CashFlow] Outflow added.");
            }
            else
            {
                Console.WriteLine($"[DEBUG CashFlow] AmountPaid={document.AmountPaid} <= 0, no CashFlow created.");
            }
            #endregion

            #region Quy tắc sinh CashFlow Rounding nếu có phần dư làm tròn
            if (roundingValue != 0)
            {
                var roundingCashFlow = new CashFlow
                {
                    BranchId = document.BranchId,
                    DocumentId = document.Id,
                    PartnerId = document.PartnerId,
                    Code = $"CF_RND_{document.Code}",
                    BusinessDate = DateTime.UtcNow,
                    Direction = CashFlowDirection.Outflow,
                    Type = CashFlowDetailType.Rounding,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = roundingValue,
                    Note = $"Điều chỉnh làm tròn (Rounding) phiếu nhập kho #{document.Code}",
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddCashFlowAsync(roundingCashFlow);
            }
            #endregion
        }
        #endregion

        #region Quản lý Snapshot Master Data (Freeze & Clear Snapshots)
        /// <summary>
        /// Xóa rỗng tất cả các trường Snapshot khi chứng từ ở trạng thái Lưu tạm (Pending).
        /// </summary>
        private static void ClearPendingSnapshots(Models.Document document)
        {
            document.SnapshotBranchName = string.Empty;
            document.SnapshotToBranchName = null;
            document.SnapshotPartnerName = null;
            document.SnapshotCreatedByName = null;
            document.SnapshotCreatedByUsername = null;
            document.SnapshotPostedByName = null;
            document.SnapshotPostedByUsername = null;
            document.SnapshotDeletedByName = null;

            if (document.DocumentDetails != null)
            {
                foreach (var detail in document.DocumentDetails)
                {
                    detail.SnapshotProductName = string.Empty;
                    detail.SnapshotProductCode = string.Empty;
                    detail.SnapshotUnitName = string.Empty;
                    detail.SnapshotBaseUnitName = string.Empty;
                }
            }
        }

        /// <summary>
        /// Đổ bê tông (Đóng băng) Master Data Snapshot khi Tạo thẳng, Chốt kho (Completed), hoặc Xóa mềm (Canceled).
        /// </summary>
        private async Task FreezeDocumentSnapshotsAsync(Models.Document document, long? actionUserId, bool isSoftDelete = false)
        {
            if (document == null) return;

            #region 1. Snapshot Chi nhánh
            if (document.BranchId > 0)
            {
                var branch = await _repo.GetBranchByIdAsync(document.BranchId);
                if (branch != null)
                {
                    document.SnapshotBranchName = branch.Name;
                }
            }

            if (document.ToBranchId.HasValue && document.ToBranchId.Value > 0)
            {
                var toBranch = await _repo.GetBranchByIdAsync(document.ToBranchId.Value);
                if (toBranch != null)
                {
                    document.SnapshotToBranchName = toBranch.Name;
                }
            }
            #endregion

            #region 2. Snapshot Nhà cung cấp / Đối tác
            if (document.PartnerId.HasValue && document.PartnerId.Value > 0)
            {
                var partner = await _repo.GetPartnerByIdAsync(document.PartnerId.Value);
                if (partner != null)
                {
                    document.SnapshotPartnerName = partner.Name;
                }
            }
            #endregion

            #region 3. Snapshot Người tạo (CreatedBy)
            if (document.CreatedBy.HasValue && document.CreatedBy.Value > 0)
            {
                var creator = await _repo.GetAccountByIdAsync(document.CreatedBy.Value);
                if (creator != null)
                {
                    document.SnapshotCreatedByName = creator.Name;
                    document.SnapshotCreatedByUsername = creator.Email;
                }
            }
            #endregion

            #region 4. Snapshot Người chốt (PostedBy) hoặc Người xóa (DeletedBy)
            if (isSoftDelete)
            {
                if (actionUserId.HasValue && actionUserId.Value > 0)
                {
                    var deleter = await _repo.GetAccountByIdAsync(actionUserId.Value);
                    if (deleter != null)
                    {
                        document.SnapshotDeletedByName = deleter.Name;
                    }
                }
            }
            else
            {
                if (document.PostedBy.HasValue && document.PostedBy.Value > 0)
                {
                    var poster = await _repo.GetAccountByIdAsync(document.PostedBy.Value);
                    if (poster != null)
                    {
                        document.SnapshotPostedByName = poster.Name;
                        document.SnapshotPostedByUsername = poster.Email;
                    }
                }
            }
            #endregion

            #region 5. Snapshot Chi tiết chứng từ (DocumentDetails)
            if (document.DocumentDetails != null)
            {
                foreach (var detail in document.DocumentDetails)
                {
                    var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                    if (binventory != null && binventory.Product != null)
                    {
                        detail.SnapshotProductName = binventory.Product.Name;
                        detail.SnapshotProductCode = binventory.Product.SKUCode;
                        detail.SnapshotBaseUnitName = binventory.Product.Name;

                        if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                        {
                            var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                            if (unitConversion != null)
                            {
                                detail.ConversionRate = unitConversion.ConversionPoint > 0 ? unitConversion.ConversionPoint : 1;
                                detail.SnapshotUnitName = unitConversion.Unit?.Name ?? binventory.Product.Name;
                            }
                            else
                            {
                                detail.ConversionRate = 1;
                                detail.SnapshotUnitName = binventory.Product.Name;
                            }
                        }
                        else
                        {
                            detail.ConversionRate = 1;
                            detail.SnapshotUnitName = binventory.Product.Name;
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Tạo phiếu Nhập kho ở trạng thái Pending (Lưu tạm)
        public async Task<Models.Document> CreateImportPendingAsync(Models.Document document, long userId)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ nhập kho không được để trống.");
            }

            document.Type = DocumentType.Import;
            document.Status = DocumentStatus.Pending;
            document.CreatedBy = userId;
            document.CreatedAt = DateTime.UtcNow;

            await ValidateImportBaseRulesAsync(document);
            ClearPendingSnapshots(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateImportCodeAsync(document);
            return document;
        }
        #endregion

        #region Cập nhật phiếu Nhập kho Pending
        public async Task<Models.Document> UpdateImportPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Import)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Nhập kho với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chỉnh sửa khi chứng từ ở trạng thái Pending.");
            }

            updatedDocument.Type = DocumentType.Import;
            await ValidateImportBaseRulesAsync(updatedDocument);
            ClearPendingSnapshots(updatedDocument);

            existing.Note = updatedDocument.Note;
            existing.PartnerId = updatedDocument.PartnerId;
            existing.OrderDate = updatedDocument.OrderDate;
            existing.TotalAmount = updatedDocument.TotalAmount;
            existing.AmountDue = updatedDocument.AmountDue;
            existing.AmountPaid = updatedDocument.AmountPaid;
            existing.ImageUrls = updatedDocument.ImageUrls;

            if (existing.DocumentDetails != null && existing.DocumentDetails.Any())
            {
                _repo.RemoveDetails(existing.DocumentDetails.ToList());
                existing.DocumentDetails.Clear();
            }
            else if (existing.DocumentDetails == null)
            {
                existing.DocumentDetails = new List<Models.DocumentDetail>();
            }

            if (updatedDocument.DocumentDetails != null)
            {
                foreach (var detail in updatedDocument.DocumentDetails)
                {
                    existing.DocumentDetails.Add(detail);
                }
            }
            ClearPendingSnapshots(existing);

            if (!string.IsNullOrWhiteSpace(updatedDocument.Code) && updatedDocument.Code != existing.Code)
            {
                existing.Code = updatedDocument.Code;
                await GenerateOrValidateImportCodeAsync(existing);
            }

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }
        #endregion

        #region Chốt phiếu Nhập kho sang Completed (Chốt kho từ phiếu Pending)
        public async Task<Models.Document> CompleteImportAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Import)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Nhập kho với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            await ValidateImportCompletedRulesAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await GenerateOrValidateImportCodeAsync(existing);
            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessWeightedAvgCostAndLedgerAsync(existing, userId);
            await CreateImportCashFlowAsync(existing);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }
        #endregion

        #region Tạo & Chốt trực tiếp phiếu Nhập kho (Lưu thẳng / Post)
        public async Task<Models.Document> CreateImportCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.Import;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateImportCompletedRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateImportCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessWeightedAvgCostAndLedgerAsync(document, userId);
            await CreateImportCashFlowAsync(document);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion

        #region Xóa mềm phiếu Nhập kho Pending
        public async Task<bool> SoftDeleteImportPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.Import, userId, deleteNote);
        }
        #endregion

        #region Lấy danh sách phiếu Nhập kho
        public async Task<IEnumerable<Models.Document>> GetImportListAsync(long branchId)
        {
            return await _repo.GetImportDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 2. Nghiệp vụ Chứng từ Trả hàng NCC (Return)

        #region Tạo & Chốt trực tiếp phiếu Trả hàng NCC
        /// <summary>
        /// Tạo & Chốt trực tiếp phiếu Trả hàng NCC (DocumentType.Return = 2, DocumentStatus.Completed).
        /// Quy tắc:
        /// - Bắt buộc trỏ ParentDocumentId đến phiếu Nhập kho gốc (Import) ở trạng thái Completed.
        /// - Các dòng chi tiết DocumentDetail phải có FatherId trỏ đến DocumentDetail tương ứng của phiếu Nhập gốc.
        /// - Kiểm soát lũy kế số lượng đã trả: Trả không quá (Số nhập gốc - Tổng số đã trả ở các phiếu Return trước).
        /// - Dòng chi tiết có Quantity <= 0 sẽ bị loại bỏ.
        /// - Thừa hưởng đơn vị tính, ConversionRate, BInventoryId, ProductId trực tiếp từ snapshot phiếu Nhập gốc.
        /// - Đơn giá UnitPrice: Sử dụng detail.UnitPrice (nếu > 0) hoặc lấy từ fatherDetail.UnitPrice.
        /// - Kiểm tra tồn kho BInventory hiện tại tại chi nhánh. Nếu tồn kho không đủ -> Báo lỗi.
        /// - Trừ kho BInventory, triệt tiêu LeftOver = 0 nếu tồn kho về 0.
        /// - Ghi thẻ Sổ kho InventoryLedger (QuantityDelta âm).
        /// - Tự động tạo phiếu Thu tiền CashFlow (Inflow / Refund) từ NCC nếu AmountPaid > 0.
        /// </summary>
        public async Task<Models.Document> CreateReturnCompletedAsync(Models.Document document, long userId)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ trả hàng không được để trống.");
            }

            if (!document.ParentDocumentId.HasValue || document.ParentDocumentId.Value <= 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentParentRequired, "Phiếu trả hàng NCC bắt buộc phải liên kết với 1 phiếu Nhập kho gốc (ParentDocumentId).");
            }

            var parentImport = await _repo.GetByIdAsync(document.ParentDocumentId.Value);
            if (parentImport == null || parentImport.Type != DocumentType.Import)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy phiếu Nhập kho gốc ID: {document.ParentDocumentId.Value}");
            }

            if (parentImport.Status != DocumentStatus.Completed)
            {
                throw new MenuGoException(ErrorCodes.DocumentParentNotCompleted, $"Phiếu Nhập kho gốc #{parentImport.Code} chưa ở trạng thái chốt kho (Completed). Không thể thực hiện trả hàng.");
            }

            #region Kiểm tra ghi chú Note & DeleteNote <= 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            // Gán thông tin cơ bản cho phiếu Return
            document.Type = DocumentType.Return;
            document.Status = DocumentStatus.Completed;
            document.BranchId = parentImport.BranchId;
            document.PartnerId = parentImport.PartnerId;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            #region Kiểm tra và làm sạch các dòng chi tiết trả hàng
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu trả hàng bắt buộc phải có ít nhất 1 mặt hàng.");
            }

            // Loại bỏ các dòng chi tiết có số lượng trả <= 0
            var validDetails = document.DocumentDetails.Where(d => d.Quantity > 0).ToList();
            if (validDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu trả hàng bắt buộc phải có ít nhất 1 mặt hàng có số lượng trả lớn hơn 0.");
            }

            // Lấy tổng số lượng đã trả tích lũy theo FatherId của phiếu nhập gốc
            var previouslyReturnedMap = await _repo.GetPreviouslyReturnedQuantitiesAsync(parentImport.Id);

            decimal totalDocumentAmount = 0m;

            foreach (var detail in validDetails)
            {
                if (!detail.FatherId.HasValue || detail.FatherId.Value <= 0)
                {
                    throw new MenuGoException(ErrorCodes.DocumentFatherIdRequired, "Mỗi mặt hàng trong phiếu trả hàng bắt buộc phải chỉ định FatherId trỏ đến chi tiết phiếu Nhập gốc.");
                }

                var fatherDetail = parentImport.DocumentDetails?.FirstOrDefault(d => d.Id == detail.FatherId.Value);
                if (fatherDetail == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentParentDetailNotFound, $"Không tìm thấy dòng chi tiết phiếu nhập gốc ID: {detail.FatherId.Value} trong phiếu nhập #{parentImport.Code}.");
                }

                // Sao chép trực tiếp Snapshot master data & đơn vị tính từ dòng gốc
                detail.BInventoryId = fatherDetail.BInventoryId;
                detail.UnitConversionId = fatherDetail.UnitConversionId;
                detail.ConversionRate = fatherDetail.ConversionRate > 0 ? fatherDetail.ConversionRate : 1m;
                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
                detail.SnapshotProductName = fatherDetail.SnapshotProductName;
                detail.SnapshotProductCode = fatherDetail.SnapshotProductCode;
                detail.SnapshotUnitName = fatherDetail.SnapshotUnitName;
                detail.SnapshotBaseUnitName = fatherDetail.SnapshotBaseUnitName;

                // Đơn giá: Người dùng tự chọn/nhập (nếu > 0), nếu <= 0 thì lấy từ fatherDetail
                if (detail.UnitPrice <= 0)
                {
                    detail.UnitPrice = fatherDetail.UnitPrice;
                }

                if (detail.UnitPrice > 10_000_000_000m)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice, $"Đơn giá trả hàng của mặt hàng '{fatherDetail.SnapshotProductName}' ({detail.UnitPrice:N0} VNĐ) không hợp lệ. Đơn giá tối đa 10 tỷ VNĐ.");
                }

                // Kiểm tra giới hạn số lượng trả tối đa (Số nhập gốc - Tổng đã trả trước đó)
                decimal alreadyReturnedBaseQty = previouslyReturnedMap.GetValueOrDefault(fatherDetail.Id, 0m);
                decimal remainingReturnableBaseQty = fatherDetail.BaseQuantity - alreadyReturnedBaseQty;

                if (remainingReturnableBaseQty <= 0)
                {
                    throw new MenuGoException(ErrorCodes.DocumentReturnAllAlreadyReturned, $"Mặt hàng '{fatherDetail.SnapshotProductName}' trong phiếu nhập #{parentImport.Code} đã được trả hết toàn bộ số lượng.");
                }

                if (detail.BaseQuantity > remainingReturnableBaseQty)
                {
                    decimal remainingInUnit = fatherDetail.ConversionRate > 0 ? (remainingReturnableBaseQty / fatherDetail.ConversionRate) : remainingReturnableBaseQty;
                    throw new MenuGoException(ErrorCodes.DocumentReturnQuantityExceeded, $"Số lượng trả hàng cho mặt hàng '{fatherDetail.SnapshotProductName}' ({detail.Quantity} {fatherDetail.SnapshotUnitName}) vượt quá số lượng tối đa còn có thể trả ({remainingInUnit} {fatherDetail.SnapshotUnitName}).");
                }

                // Kiểm tra tồn kho hiện tại tại Chi nhánh
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy tồn kho BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Quantity < detail.BaseQuantity)
                {
                    throw new MenuGoException(ErrorCodes.DocumentReturnInsufficientInventory, $"Số lượng tồn kho hiện tại của mặt hàng '{fatherDetail.SnapshotProductName}' ({binventory.Quantity:N3}) không đủ để thực hiện xuất trả hàng ({detail.BaseQuantity:N3}).");
                }

                totalDocumentAmount += (detail.Quantity * detail.UnitPrice);
            }

            document.TotalAmount = totalDocumentAmount;
            document.DocumentDetails = validDetails;

            var (roundedReturnTotal, roundingReturnValue) = CalculateDocumentRounding(totalDocumentAmount);

            if (document.AmountPaid < 0 || document.AmountPaid > roundedReturnTotal)
            {
                throw new MenuGoException(ErrorCodes.DocumentAmountPaidInvalid, $"Số tiền nhà cung cấp đã trả ({document.AmountPaid:N0} VNĐ) không hợp lệ. Phải nằm trong khoảng từ 0 đến Tổng tiền hoàn trả sau làm tròn ({roundedReturnTotal:N0} VNĐ).");
            }

            document.AmountDue = Math.Max(0, roundedReturnTotal - document.AmountPaid);
            #endregion

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            #region Phát sinh Mã Chứng Từ TH (Nếu trống)
            if (string.IsNullOrWhiteSpace(document.Code) || document.Code.StartsWith("TEMP_"))
            {
                document.Code = $"TH{document.Id:D6}";
                bool isExist = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExist)
                {
                    document.Code = $"TH{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
            else
            {
                document.Code = document.Code.Trim();
                bool isExist = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExist)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống.");
                }
            }
            #endregion

            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);

            #region Trừ kho, Ghi Sổ kho InventoryLedger & Sinh CashFlow
            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory != null)
                {
                    decimal leftover_old = binventory.LeftOver;
                    decimal val_delta;

                    decimal q_return = detail.BaseQuantity;
                    binventory.Quantity -= q_return;

                    if (binventory.Quantity <= 0)
                    {
                        binventory.Quantity = 0;
                        val_delta = -((q_return * binventory.Avg) + leftover_old);
                        binventory.LeftOver = 0;
                    }
                    else
                    {
                        val_delta = -(q_return * binventory.Avg);
                    }

                    // Bản ghi Sổ kho InventoryLedger cho phiếu Trả hàng
                    var ledger = new InventoryLedger
                    {
                        BInventoryId = binventory.Id,
                        DocumentId = document.Id,
                        PostedAt = document.PostedAt ?? DateTime.UtcNow,
                        PostedBy = userId,
                        DocumentType = DocumentType.Return,
                        SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                        SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                        SnapshotProductName = !string.IsNullOrWhiteSpace(detail.SnapshotProductName) ? detail.SnapshotProductName : (binventory.Product?.Name ?? string.Empty),
                        SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventory.Product?.Name ?? string.Empty),
                        ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                        QuantityDelta = -q_return,
                        InventoryValueDelta = val_delta,
                        RunningQuantity = binventory.Quantity,
                        RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                        RunningAverageCost = binventory.Avg,
                        UnitCost = (detail.ConversionRate > 0) ? (detail.UnitPrice / detail.ConversionRate) : detail.UnitPrice,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.AddInventoryLedgerAsync(ledger);

                    // Xử lý trừ Lô hàng nhập gốc & Ghi nhận phân bổ Trả hàng NCC
                    var fatherDetail = parentImport?.DocumentDetails?.FirstOrDefault(d => d.Id == detail.FatherId);
                    string targetBatchCode = fatherDetail?.BatchCodeSnapshot ?? detail.BatchCodeSnapshot ?? $"LOT-IMP-{parentImport?.Code}-{detail.FatherId}";

                    detail.BatchCodeSnapshot = targetBatchCode;

                    var targetBatch = await _repo.GetBatchByInventoryAndCodeAsync(binventory.Id, targetBatchCode);
                    if (targetBatch != null)
                    {
                        if (q_return > targetBatch.QuantityRemaining)
                        {
                            throw new MenuGoException(ErrorCodes.DocumentReturnInsufficientInventory, $"Số lượng trả hàng ({q_return:N3}) vượt quá số lượng còn lại của Lô hàng nhập gốc '{targetBatch.BatchCode}' ({targetBatch.QuantityRemaining:N3}).");
                        }

                        targetBatch.QuantityRemaining -= q_return;
                        if (targetBatch.QuantityRemaining == 0)
                        {
                            targetBatch.Status = BatchStatus.Depleted;
                        }
                        targetBatch.UpdatedAt = DateTime.UtcNow;

                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetailId = detail.Id,
                            BatchId = targetBatch.Id,
                            AllocationType = BatchAllocationType.ReturnToSupplier,
                            QuantityAllocated = q_return,
                            UnitCost = targetBatch.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
            }

            // Sinh phiếu CashFlow Thu tiền (Refund / Inflow) từ NCC nếu AmountPaid > 0
            if (document.AmountPaid > 0 && document.PartnerId.HasValue)
            {
                var cashFlow = new CashFlow
                {
                    BranchId = document.BranchId,
                    DocumentId = document.Id,
                    PartnerId = document.PartnerId,
                    Code = $"CF_{document.Code}",
                    BusinessDate = DateTime.UtcNow,
                    Direction = CashFlowDirection.Inflow, // Thu tiền NCC hoàn trả
                    Type = CashFlowDetailType.Refund,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = document.AmountPaid,
                    Note = $"Thu tiền nhà cung cấp hoàn trả từ phiếu trả hàng #{document.Code}",
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddCashFlowAsync(cashFlow);
            }

            // Sinh phiếu CashFlow Rounding nếu có phần dư làm tròn
            if (roundingReturnValue != 0 && document.PartnerId.HasValue)
            {
                var roundingCashFlow = new CashFlow
                {
                    BranchId = document.BranchId,
                    DocumentId = document.Id,
                    PartnerId = document.PartnerId,
                    Code = $"CF_RND_{document.Code}",
                    BusinessDate = DateTime.UtcNow,
                    Direction = CashFlowDirection.Inflow,
                    Type = CashFlowDetailType.Rounding,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = CashFlowStatus.Completed,
                    TotalAmount = roundingReturnValue,
                    Note = $"Điều chỉnh làm tròn (Rounding) phiếu trả hàng #{document.Code}",
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddCashFlowAsync(roundingCashFlow);
            }
            #endregion

            #region Cập nhật công nợ còn lại (AmountDue) của phiếu Nhập kho gốc
            if (parentImport != null)
            {
                var prevReturns = await _repo.GetDocumentsAsync(parentImport.BranchId, DocumentType.Return, DocumentStatus.Completed);
                decimal totalReturnsForParent = prevReturns
                    .Where(r => r.ParentDocumentId == parentImport.Id && r.Id != document.Id)
                    .Sum(r => r.TotalAmount) + document.TotalAmount;

                decimal parentDebtDeduction = ExtractDebtDeduction(parentImport.Note);
                decimal parentEffectivePayable = Math.Max(0m, parentImport.TotalAmount - totalReturnsForParent);
                parentImport.AmountDue = Math.Max(0m, (parentEffectivePayable - parentDebtDeduction) - parentImport.AmountPaid);
                _repo.Update(parentImport);
            }
            #endregion

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion

        #region Lấy danh sách phiếu Trả hàng NCC
        public async Task<IEnumerable<Models.Document>> GetReturnListAsync(long branchId)
        {
            return await _repo.GetReturnDocumentsAsync(branchId);
        }
        #endregion

        #region Lấy thông tin chi tiết phiếu Nhập kho cho tạo phiếu Trả hàng
        /// <summary>
        /// Lấy thông tin dòng chi tiết của phiếu Nhập kho (đã Completed) kèm số lượng đã trả tích lũy.
        /// Toàn bộ dữ liệu sản phẩm lấy từ Snapshot của phiếu Nhập đã chốt (không phụ thuộc master data hiện tại).
        /// </summary>
        public async Task<Dtos.Document.ImportReturnDetailsDto?> GetImportReturnDetailsAsync(long importDocumentId)
        {
            var importDoc = await _repo.GetByIdAsync(importDocumentId);
            if (importDoc == null || importDoc.Type != DocumentType.Import)
            {
                return null;
            }

            if (importDoc.Status != DocumentStatus.Completed)
            {
                throw new MenuGoException(ErrorCodes.DocumentParentNotCompleted, $"Phiếu Nhập kho #{importDoc.Code} chưa ở trạng thái chốt kho (Completed). Chỉ có thể trả hàng cho phiếu đã chốt.");
            }

            // Lấy số lượng đã trả tích lũy theo từng FatherId (dòng chi tiết phiếu Nhập gốc)
            var previouslyReturnedMap = await _repo.GetPreviouslyReturnedQuantitiesAsync(importDoc.Id);

            var result = new Dtos.Document.ImportReturnDetailsDto
            {
                ImportDocumentId = importDoc.Id,
                ImportDocumentCode = importDoc.Code ?? string.Empty,
                PartnerId = importDoc.PartnerId,
                // Tên NCC: dùng Snapshot nếu có
                PartnerName = importDoc.SnapshotPartnerName ?? string.Empty,
                TotalAmount = importDoc.TotalAmount,
                AmountPaid = importDoc.AmountPaid,
                Details = new List<Dtos.Document.ImportReturnDetailItemDto>()
            };

            if (importDoc.DocumentDetails != null)
            {
                foreach (var detail in importDoc.DocumentDetails)
                {
                    // Số lượng (BaseQuantity) đã trả tích lũy theo dòng này
                    decimal alreadyReturnedBase = previouslyReturnedMap.GetValueOrDefault(detail.Id, 0m);

                    // Số lượng BaseQuantity còn được trả
                    decimal remainingBase = Math.Max(0m, detail.BaseQuantity - alreadyReturnedBase);

                    // Quy về đơn vị tính (chia cho ConversionRate)
                    decimal convRate = detail.ConversionRate > 0 ? detail.ConversionRate : 1m;
                    decimal maxReturnInUnit = Math.Round(remainingBase / convRate, 6);

                    result.Details.Add(new Dtos.Document.ImportReturnDetailItemDto
                    {
                        Id = detail.Id,
                        BInventoryId = detail.BInventoryId,
                        UnitConversionId = detail.UnitConversionId,
                        // Hoàn toàn dựa vào Snapshot của phiếu Nhập đã chốt
                        ProductName = detail.SnapshotProductName ?? string.Empty,
                        ProductCode = detail.SnapshotProductCode ?? string.Empty,
                        UnitName = detail.SnapshotUnitName ?? string.Empty,
                        ConversionRate = convRate,
                        Quantity = detail.Quantity,
                        BaseQuantity = detail.BaseQuantity,
                        UnitPrice = detail.UnitPrice,
                        AlreadyReturnedBaseQuantity = alreadyReturnedBase,
                        MaxReturnQuantity = maxReturnInUnit
                    });
                }
            }

            return result;
        }
        #endregion

        #endregion

        #region 3. Nghiệp vụ Chứng từ Xuất bán hàng (Sale)

        #region DTO Nội Bộ Cho Trừ Kho Xuất Bán
        private class SaleDeductionItem
        {
            public long ProductId { get; set; }
            public Product Product { get; set; } = null!;
            public decimal RequiredBaseQuantity { get; set; }
            public long ParentDetailId { get; set; }
        }
        #endregion

        #region Validation Nghiệp Vụ Phiếu Xuất Bán (Sale Validation Rules)
        /// <summary>
        /// Kiểm tra các quy tắc bắt buộc cho phiếu Xuất bán hàng:
        /// - Note & DeleteNote <= 255 ký tự
        /// - Tối thiểu 1 dòng chi tiết (DocumentDetail)
        /// - Mặt hàng thuộc 3 loại: Regular, Manufactured, Processed
        /// - Số lượng 0.001 <= Quantity <= 10 tỷ
        /// - Đơn giá 0 <= UnitPrice <= 10 tỷ
        /// - Tổng tiền đơn bán: TotalAmount <= 10 tỷ VNĐ
        /// </summary>
        private async Task ValidateSaleBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ xuất bán không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Danh sách mặt hàng xuất bán
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu xuất bán bắt buộc phải có ít nhất 1 mặt hàng.");
            }

            const decimal MIN_QTY = 0.001m;
            const decimal MAX_QTY = 10_000_000_000m;
            const decimal MAX_PRICE = 10_000_000_000m;
            const decimal MAX_TOTAL = 10_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy mặt hàng BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                var productType = binventory.Product.Type;
                if (productType != ProductType.Regular && productType != ProductType.Manufactured && productType != ProductType.Processed)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' thuộc loại '{productType}', không được phép xuất bán trực tiếp. Đơn bán chỉ chấp nhận 3 loại: Regular, Manufactured, Processed.");
                }

                if (detail.Quantity < MIN_QTY || detail.Quantity > MAX_QTY)
                {
                    throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng xuất bán của mặt hàng '{binventory.Product.Name}' ({detail.Quantity}) không hợp lệ. Phải nằm trong khoảng từ 0.001 đến 10 tỷ.");
                }

                if (detail.UnitPrice < 0 || detail.UnitPrice > MAX_PRICE)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice, $"Đơn giá xuất bán của mặt hàng '{binventory.Product.Name}' ({detail.UnitPrice:N0} VNĐ) không hợp lệ. Đơn giá phải từ 0 đến 10 tỷ VNĐ.");
                }

                #region Đơn vị tính và UnitConversion
                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion == null || unitConversion.ProductId != binventory.ProductId)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Đơn vị quy đổi ID {detail.UnitConversionId.Value} không tồn tại hoặc không thuộc về sản phẩm '{binventory.Product.Name}'.");
                    }

                    if (unitConversion.ConversionPoint <= 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Tỷ lệ quy đổi của đơn vị tính ID {detail.UnitConversionId.Value} phải lớn hơn 0.");
                    }

                    detail.ConversionRate = unitConversion.ConversionPoint;
                }
                else
                {
                    detail.ConversionRate = 1;
                }
                #endregion

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;

                // Tự động gán UnitPrice theo giá bán (SellPrice) của sản phẩm nếu chưa truyền hoặc <= 0
                if (detail.UnitPrice <= 0 && binventory.Product.SellPrice > 0)
                {
                    detail.UnitPrice = binventory.Product.SellPrice * detail.ConversionRate;
                }
            }
            #endregion

            decimal calculatedTotalAmount = document.DocumentDetails.Sum(d => d.Quantity * d.UnitPrice);
            if (calculatedTotalAmount < 0 || calculatedTotalAmount > MAX_TOTAL)
            {
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit, $"Tổng giá trị phiếu xuất bán ({calculatedTotalAmount:N0} VNĐ) vượt quá hạn mức tối đa 10 tỷ VNĐ.");
            }

            document.TotalAmount = calculatedTotalAmount;
            if (!document.AmountDue.HasValue)
            {
                document.AmountDue = Math.Max(0, document.TotalAmount - document.AmountPaid);
            }
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Xuất bán dựa trên ID (Tiền tố BH).
        /// </summary>
        private async Task GenerateOrValidateSaleCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"BH{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"BH{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        #region Thuật toán Trừ kho Bán hàng (Sale Inventory Deduction Logic)

        /// <summary>
        /// BƯỚC 1: Kiểm tra Vòng lặp Công thức (Circular Dependency Check - DAG Check)
        /// Ngừng giao dịch & Thăng ném lỗi lập tức nếu bất kỳ sản phẩm Processed nào tự trỏ đến chính nó (trực tiếp hoặc qua trung gian).
        /// </summary>
        private async Task ValidateCircularDependencyAsync(long processedProductId, HashSet<long> ancestorPath)
        {
            if (ancestorPath.Contains(processedProductId))
            {
                throw new MenuGoException(ErrorCodes.DocumentCircularDependency, $"Phát hiện VÒNG LẶP CÔNG THỨC (Circular Dependency) đối với sản phẩm Chế biến ID {processedProductId}. Công thức bị lặp đệ quy tự trỏ đến chính nó.");
            }

            ancestorPath.Add(processedProductId);

            var recipes = await _repo.GetRecipesByParentProductIdAsync(processedProductId);
            if (recipes == null || recipes.Count == 0) return;

            foreach (var recipe in recipes)
            {
                var ingredientProduct = recipe.IngredientProduct;
                if (ingredientProduct != null && ingredientProduct.Type == ProductType.Processed)
                {
                    await ValidateCircularDependencyAsync(recipe.IngredientProductId, new HashSet<long>(ancestorPath));
                }
            }
        }

        /// <summary>
        /// BƯỚC 2: Phân rã Công thức Sâu (Deep Recipe Unrolling)
        /// - Regular -> Trừ trực tiếp
        /// - Manufactured -> TRỪ TRỰC TIẾP, TUYỆT ĐỐI KHÔNG BUNG CÔNG THỨC
        /// - Processed -> Bung công thức đệ quy sâu, dừng tại INGREDIENT, MANUFACTURED, REGULAR
        /// </summary>
        private async Task UnrollProcessedProductRecipeAsync(long processedProductId, decimal multiplierQty, long parentDetailId, List<SaleDeductionItem> deductionList, HashSet<long>? visitedProductIds = null)
        {
            visitedProductIds ??= new HashSet<long>();
            if (!visitedProductIds.Add(processedProductId))
            {
                throw new MenuGoException(ErrorCodes.DocumentCircularDependency, $"Phát hiện vòng lặp phụ thuộc (Circular Dependency) trong công thức của sản phẩm chế biến ID {processedProductId}.");
            }

            var recipes = await _repo.GetRecipesByParentProductIdAsync(processedProductId);
            if (recipes == null || recipes.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentRecipeNotFound, $"Sản phẩm chế biến (Processed) ID {processedProductId} không có công thức chi tiết (Recipe).");
            }

            foreach (var recipe in recipes)
            {
                decimal neededQty = recipe.Quantity * multiplierQty;

                var ingredientProduct = recipe.IngredientProduct;

                if (ingredientProduct == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentIngredientNotFound, $"Không tìm thấy sản phẩm nguyên liệu ID {recipe.IngredientProductId} trong công thức.");
                }

                if (ingredientProduct.Type == ProductType.Processed)
                {
                    await UnrollProcessedProductRecipeAsync(recipe.IngredientProductId, neededQty, parentDetailId, deductionList, visitedProductIds);
                }
                else
                {
                    deductionList.Add(new SaleDeductionItem
                    {
                        ProductId = recipe.IngredientProductId,
                        Product = ingredientProduct,
                        RequiredBaseQuantity = neededQty,
                        ParentDetailId = parentDetailId
                    });
                }
            }
        }

        /// <summary>
        /// Thực thi chốt phiếu Xuất bán Completed và Trừ kho:
        /// 1. Validate DAG Circular Dependency.
        /// 2. Phân rã & Tính toán nhu cầu tiêu hao (Unroll & Aggregate).
        /// 3. Gộp nhu cầu (Consolidate).
        /// 4. Kiểm tra tồn kho & Trừ kho hàng loạt (Batch Deduction + LeftOver + Ledger + Child Details).
        /// </summary>
        private async Task ProcessSaleCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            // BƯỚC 1: VALIDATE (Kiểm tra vòng lặp công thức cho toàn bộ sản phẩm PROCESSED)
            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory?.Product != null && binventory.Product.Type == ProductType.Processed)
                {
                    await ValidateCircularDependencyAsync(binventory.ProductId, new HashSet<long>());
                }
            }

            // BƯỚC 2: UNROLL & AGGREGATE (Phân rã & Tính toán nhu cầu)
            var rawDeductions = new List<SaleDeductionItem>();

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null || binventory.Product == null) continue;

                var product = binventory.Product;

                if (product.Type == ProductType.Regular || product.Type == ProductType.Manufactured)
                {
                    // REGULAR & MANUFACTURED: Trừ trực tiếp, KHÔNG bung công thức
                    rawDeductions.Add(new SaleDeductionItem
                    {
                        ProductId = binventory.ProductId,
                        Product = product,
                        RequiredBaseQuantity = detail.BaseQuantity,
                        ParentDetailId = detail.Id
                    });
                }
                else if (product.Type == ProductType.Processed)
                {
                    // PROCESSED: Bung công thức sâu đến tận cùng
                    await UnrollProcessedProductRecipeAsync(binventory.ProductId, detail.BaseQuantity, detail.Id, rawDeductions);
                }
            }

            // BƯỚC 3: CONSOLIDATE (Gộp dữ liệu theo mặt hàng)
            var consolidatedDeductions = rawDeductions
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Product = g.First().Product,
                    TotalRequiredQuantity = g.Sum(x => x.RequiredBaseQuantity),
                    ParentDetailIds = g.Select(x => x.ParentDetailId).Distinct().ToList()
                })
                .ToList();

            // BƯỚC 4: BATCH DEDUCTION (Kiểm tra tồn & Trừ kho 1 lần)
            var createdChildDetails = new List<DocumentDetail>();

            foreach (var item in consolidatedDeductions)
            {
                var binventory = await _repo.GetBInventoryByBranchAndProductAsync(document.BranchId, item.ProductId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Mặt hàng '{item.Product.Name}' chưa có trong kho của Chi nhánh này.");
                }

                if (binventory.Quantity < item.TotalRequiredQuantity)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInsufficientInventory, $"Tồn kho chi nhánh không đủ để xuất bán sản phẩm '{item.Product.Name}'. Yêu cầu: {item.TotalRequiredQuantity:N3}, Tồn hiện tại: {binventory.Quantity:N3}.");
                }

                decimal leftover_old = binventory.LeftOver;
                decimal val_delta;

                binventory.Quantity -= item.TotalRequiredQuantity;

                if (binventory.Quantity == 0)
                {
                    val_delta = -((item.TotalRequiredQuantity * binventory.Avg) + leftover_old);
                    binventory.LeftOver = 0;
                }
                else
                {
                    val_delta = -(item.TotalRequiredQuantity * binventory.Avg);
                }

                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Sale,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = item.Product.Name,
                    SnapshotUnitName = item.Product.Name,
                    ConversionRateSnapshot = 1m,
                    QuantityDelta = -item.TotalRequiredQuantity,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = binventory.Quantity,
                    RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                    RunningAverageCost = binventory.Avg,
                    UnitCost = binventory.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);

                // Phân bổ Lô hàng theo chuẩn FEFO
                IReadOnlyList<FefoAllocationResult> fefoAllocations = new List<FefoAllocationResult>();
                if (_fefoService != null)
                {
                    fefoAllocations = await _fefoService.AllocateAsync(binventory.Id, item.TotalRequiredQuantity);
                }

                DocumentDetail? createdOrMatchedDetail = null;
                foreach (var parentId in item.ParentDetailIds)
                {
                    var parentDetail = document.DocumentDetails.FirstOrDefault(d => d.Id == parentId);
                    if (parentDetail != null)
                    {
                        if (parentDetail.BInventoryId != binventory.Id)
                        {
                            var childDetail = new DocumentDetail
                            {
                                DocumentId = document.Id,
                                FatherId = parentDetail.Id,
                                BInventoryId = binventory.Id,
                                Quantity = item.TotalRequiredQuantity,
                                ConversionRate = 1,
                                BaseQuantity = item.TotalRequiredQuantity,
                                UnitPrice = binventory.Avg,
                                SnapshotProductName = item.Product.Name,
                                SnapshotAvgCost = binventory.Avg
                            };
                            document.DocumentDetails.Add(childDetail);
                            createdOrMatchedDetail = childDetail;
                        }
                        else
                        {
                            createdOrMatchedDetail = parentDetail;
                        }
                    }
                }

                createdOrMatchedDetail ??= document.DocumentDetails.FirstOrDefault(d => d.BInventoryId == binventory.Id);

                if (createdOrMatchedDetail != null && fefoAllocations.Any())
                {
                    foreach (var alloc in fefoAllocations)
                    {
                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetail = createdOrMatchedDetail,
                            BatchId = alloc.BatchId,
                            AllocationType = BatchAllocationType.Sale,
                            QuantityAllocated = alloc.QuantityAllocated,
                            UnitCost = alloc.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
            }
        }
        #endregion

        #region API Handlers cho Sale
        public async Task<Models.Document> CreateSaleCompletedAsync(Models.Document document, long userId)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ xuất bán không được để trống.");
            }

            document.Type = DocumentType.Sale;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateSaleBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateSaleCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessSaleCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<IEnumerable<Models.Document>> GetSaleListAsync(long branchId)
        {
            return await _repo.GetSaleDocumentsAsync(branchId);
        }

        /// <summary>
        /// Tạo phiếu Xuất bán chỉ ghi nhận tài chính, KHÔNG trừ kho.
        /// Dùng khi hàng đã được trừ kho trực tiếp tại OrderDetailService (Confirm/Cooking).
        /// </summary>
        public async Task<Models.Document> CreateSaleRecordOnlyAsync(Models.Document document, long userId)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ xuất bán không được để trống.");
            }

            document.Type = DocumentType.Sale;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateSaleBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateSaleCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            // KHÔNG gọi ProcessSaleCompletedAsync → Không trừ kho lần nữa

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion

        #region Real-time Incremental Documents cho Order

        public async Task<Models.Document> GetOrCreatePendingDocumentAsync(long branchId, long orderId, DocumentType type, long userId)
        {
            var existingDoc = await _repo.GetPendingDocumentByOrderIdAsync(orderId, type);
            if (existingDoc != null) return existingDoc;

            var newDoc = new Models.Document
            {
                BranchId = branchId,
                OrderId = orderId,
                Type = type,
                Status = DocumentStatus.Pending,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                OrderDate = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                Code = $"{(type == DocumentType.Sale ? "PXBH" : "PTH")}-{orderId}-{DateTime.UtcNow.ToString("HHmmssfff")}"
            };

            await _repo.AddAsync(newDoc);
            await _repo.SaveChangesAsync();
            return newDoc;
        }

        public async Task AppendItemToSaleDocumentAsync(long documentId, Product product, decimal baseQuantity, long parentDetailId, long userId)
        {
            var document = await _repo.GetByIdAsync(documentId);
            if (document == null) throw new Exception("Không tìm thấy Sale Document.");

            var rawDeductions = new List<SaleDeductionItem>();

            if (product.Type == ProductType.Regular || product.Type == ProductType.Manufactured)
            {
                rawDeductions.Add(new SaleDeductionItem
                {
                    ProductId = product.Id,
                    Product = product,
                    RequiredBaseQuantity = baseQuantity,
                    ParentDetailId = parentDetailId
                });
            }
            else if (product.Type == ProductType.Processed)
            {
                await ValidateCircularDependencyAsync(product.Id, new HashSet<long>());
                await UnrollProcessedProductRecipeAsync(product.Id, baseQuantity, parentDetailId, rawDeductions);
            }

            var consolidatedDeductions = rawDeductions
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Product = g.First().Product,
                    TotalRequiredQuantity = g.Sum(x => x.RequiredBaseQuantity),
                    ParentDetailIds = g.Select(x => x.ParentDetailId).Distinct().ToList()
                })
                .ToList();

            foreach (var item in consolidatedDeductions)
            {
                var binventory = await _repo.GetBInventoryByBranchAndProductAsync(document.BranchId, item.ProductId);
                if (binventory == null)
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Mặt hàng '{item.Product.Name}' chưa có trong kho.");

                decimal val_delta = 0m;
                if (binventory.IsManageQuantity)
                {
                    if (binventory.Quantity < item.TotalRequiredQuantity)
                        throw new MenuGoException(ErrorCodes.DocumentInsufficientInventory, $"Không đủ tồn kho '{item.Product.Name}'. Yêu cầu: {item.TotalRequiredQuantity:N3}, Tồn: {binventory.Quantity:N3}");

                    decimal leftover_old = binventory.LeftOver;
                    binventory.Quantity -= item.TotalRequiredQuantity;

                    if (binventory.Quantity == 0)
                    {
                        val_delta = -((item.TotalRequiredQuantity * binventory.Avg) + leftover_old);
                        binventory.LeftOver = 0;
                    }
                    else
                    {
                        val_delta = -(item.TotalRequiredQuantity * binventory.Avg);
                    }
                }

                var account = await _repo.GetAccountByIdAsync(userId);
                string snapshotPostedByName = account != null ? account.Name : "System";

                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Sale,
                    SnapshotPostedByName = snapshotPostedByName,
                    SnapshotBranchName = document.SnapshotBranchName ?? "",
                    SnapshotProductName = item.Product.Name,
                    SnapshotUnitName = item.Product.Name,
                    ConversionRateSnapshot = 1m,
                    QuantityDelta = -item.TotalRequiredQuantity,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = binventory.Quantity,
                    RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                    RunningAverageCost = binventory.Avg,
                    UnitCost = binventory.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);

                // Phân bổ Lô hàng theo chuẩn FEFO cho Sale POS
                IReadOnlyList<FefoAllocationResult> fefoAllocations = new List<FefoAllocationResult>();
                if (_fefoService != null)
                {
                    fefoAllocations = await _fefoService.AllocateAsync(binventory.Id, item.TotalRequiredQuantity);
                }

                var childDetail = new DocumentDetail
                {
                    DocumentId = document.Id,
                    FatherId = null,
                    BInventoryId = binventory.Id,
                    Quantity = item.TotalRequiredQuantity,
                    ConversionRate = 1,
                    BaseQuantity = item.TotalRequiredQuantity,
                    UnitPrice = binventory.Avg,
                    SnapshotProductName = item.Product.Name,
                    SnapshotAvgCost = binventory.Avg
                };
                document.DocumentDetails.Add(childDetail);

                if (fefoAllocations.Any())
                {
                    foreach (var alloc in fefoAllocations)
                    {
                        var batchAlloc = new BatchAllocation
                        {
                            // childDetail.Id chưa được EF gán (chưa SaveChanges) — dùng navigation
                            // property để EF tự fix-up FK khi save, thay vì gán thẳng Id = 0.
                            DocumentDetail = childDetail,
                            BatchId = alloc.BatchId,
                            AllocationType = BatchAllocationType.Sale,
                            QuantityAllocated = alloc.QuantityAllocated,
                            UnitCost = alloc.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
            }
            _repo.Update(document);
            await _repo.SaveChangesAsync();
        }

        public async Task AppendItemToReturnDocumentAsync(long documentId, Product product, decimal baseQuantity, bool isIntact, string returnReason, long parentDetailId, long userId)
        {
            var document = await _repo.GetByIdAsync(documentId);
            if (document == null) throw new Exception("Không tìm thấy Return Document.");

            var binventory = await _repo.GetBInventoryByBranchAndProductAsync(document.BranchId, product.Id);
            if (binventory != null)
            {
                string note = returnReason; // Lưu lý do thực tế khách/nhân viên chọn
                
                // Logic kiểm tra Regular/Manufactured và IsIntact: Chỉ cộng lại kho nếu là món Thường/Đóng gói và còn nguyên vẹn
                if ((product.Type == MenuGoBE.Models.Enums.ProductType.Regular || product.Type == MenuGoBE.Models.Enums.ProductType.Manufactured) && isIntact)
                {
                    // Cộng lại tồn kho
                    binventory.Quantity += baseQuantity;
                    
                    var account = await _repo.GetAccountByIdAsync(userId);
                    string snapshotPostedByName = account != null ? account.Name : "System";

                    // Ghi thẻ kho (Inventory Ledger)
                    var ledger = new InventoryLedger
                    {
                        BInventoryId = binventory.Id,
                        DocumentId = document.Id,
                        PostedAt = DateTime.UtcNow,
                        PostedBy = userId,
                        DocumentType = MenuGoBE.Models.Enums.DocumentType.CustomerReturn,
                        SnapshotPostedByName = snapshotPostedByName,
                        SnapshotBranchName = document.SnapshotBranchName ?? "",
                        SnapshotProductName = product.Name,
                        SnapshotUnitName = product.Name,
                        ConversionRateSnapshot = 1m,
                        QuantityDelta = baseQuantity,
                        InventoryValueDelta = baseQuantity * binventory.Avg,
                        RunningQuantity = binventory.Quantity,
                        RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                        RunningAverageCost = binventory.Avg,
                        UnitCost = binventory.Avg,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repo.AddInventoryLedgerAsync(ledger);
                    note += " (Đã cộng tồn kho)";
                }
                else
                {
                    // Nếu là Processed/Manufactured HOẶC không nguyên vẹn -> không cộng kho (Phase 1)
                    // Hàng lỗi/Đã chế biến -> Coi như xuất hủy, chỉ ghi nhận document detail để đối soát.
                    note += " (Không cộng tồn kho do hàng chế biến/không nguyên vẹn)";
                }

                // LUÔN LUÔN ghi nhận vào chi tiết phiếu trả (DocumentDetail)
                var childDetail = new DocumentDetail
                {
                    DocumentId = document.Id,
                    BInventoryId = binventory.Id,
                    Quantity = baseQuantity,
                    ConversionRate = 1,
                    BaseQuantity = baseQuantity,
                    UnitPrice = binventory.Avg,
                    SnapshotProductName = product.Name,
                    SnapshotAvgCost = binventory.Avg,
                    Note = note
                };
                document.DocumentDetails.Add(childDetail);

                // Hoàn trả số lượng vào Lô hàng (Lô Active gần nhất hoặc Lô kế thừa)
                var batches = await _repo.GetBatchesByInventoryIdAsync(binventory.Id);
                var targetBatch = batches.OrderByDescending(b => b.ReceivedDate).FirstOrDefault(b => b.Status == BatchStatus.Active) 
                               ?? batches.OrderByDescending(b => b.ReceivedDate).FirstOrDefault();
                
                if (targetBatch != null)
                {
                    targetBatch.QuantityRemaining += baseQuantity;
                    if (targetBatch.Status == BatchStatus.Depleted)
                    {
                        targetBatch.Status = BatchStatus.Active;
                    }
                    targetBatch.UpdatedAt = DateTime.UtcNow;

                    var returnAlloc = new BatchAllocation
                    {
                        DocumentDetail = childDetail,
                        BatchId = targetBatch.Id,
                        AllocationType = BatchAllocationType.CustomerReturn,
                        QuantityAllocated = baseQuantity,
                        UnitCost = targetBatch.UnitCost,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repo.AddBatchAllocationAsync(returnAlloc);
                }
            }
            
            _repo.Update(document);
            await _repo.SaveChangesAsync();
        }

        #region Chốt chứng từ của Order khi thanh toán hóa đơn
        public async Task FinalizeOrderDocumentsAsync(long orderId, decimal finalAmount, MenuGoBE.Models.Enums.PaymentMethod paymentMethod, long? partnerId, long? userId)
        {
            var saleDoc = await _repo.GetDocumentByOrderIdAsync(orderId, DocumentType.Sale);
            var order = await _repo.GetOrderWithDetailsAsync(orderId);

            long branchId = order?.Table?.Area?.BranchId ?? 1;

            if (saleDoc == null)
            {
                saleDoc = new Models.Document
                {
                    BranchId = branchId,
                    OrderId = orderId,
                    Code = $"PXBH-{orderId}",
                    Type = DocumentType.Sale,
                    Status = DocumentStatus.Completed,
                    PartnerId = partnerId,
                    TotalAmount = finalAmount,
                    AmountPaid = finalAmount,
                    CreatedBy = userId ?? order?.CreatedBy ?? 1,
                    PostedBy = userId ?? order?.CreatedBy ?? 1,
                    CreatedAt = DateTime.UtcNow,
                    PostedAt = DateTime.UtcNow,
                    Note = $"Phiếu xuất bán trừ nguyên liệu theo hóa đơn HD{orderId}"
                };
                await _repo.AddAsync(saleDoc);
                await _repo.SaveChangesAsync();
                await GenerateOrValidateSaleCodeAsync(saleDoc);
            }
            else
            {
                saleDoc.Status = DocumentStatus.Completed;
                saleDoc.TotalAmount = finalAmount;
                saleDoc.AmountPaid = finalAmount;
                if (partnerId.HasValue) saleDoc.PartnerId = partnerId.Value;
                saleDoc.PostedBy = userId ?? saleDoc.CreatedBy;
                saleDoc.PostedAt = DateTime.UtcNow;
            }

            // Bóc tách và trừ nguyên liệu theo hóa đơn đã thanh toán
            if (order != null)
            {
                var activeOrderDetails = new List<OrderDetail>();
                activeOrderDetails.AddRange(order.OrderDetails.Where(od => od.Status != "Cancelled"));
                if (order.ChildOrders != null)
                {
                    foreach (var child in order.ChildOrders)
                    {
                        activeOrderDetails.AddRange(child.OrderDetails.Where(od => od.Status != "Cancelled"));
                    }
                }

                var rawDeductions = new List<SaleDeductionItem>();
                foreach (var od in activeOrderDetails)
                {
                    if (od.Quantity <= 0) continue;
                    var prod = od.Product ?? await _repo.GetProductByIdAsync(od.ProductId);
                    if (prod == null) continue;

                    if (prod.Type == ProductType.Regular || prod.Type == ProductType.Manufactured || prod.Type == ProductType.Ingredient)
                    {
                        rawDeductions.Add(new SaleDeductionItem
                        {
                            ProductId = prod.Id,
                            Product = prod,
                            RequiredBaseQuantity = od.Quantity,
                            ParentDetailId = od.Id
                        });
                    }
                    else if (prod.Type == ProductType.Processed)
                    {
                        try
                        {
                            await ValidateCircularDependencyAsync(prod.Id, new HashSet<long>());
                            await UnrollProcessedProductRecipeAsync(prod.Id, od.Quantity, od.Id, rawDeductions);
                        }
                        catch
                        {
                            // Bỏ qua lỗi định lượng để đảm bảo chốt thanh toán không bị gián đoạn
                        }
                    }
                }

                var consolidatedDeductions = rawDeductions
                    .GroupBy(d => d.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Product = g.First().Product,
                        TotalRequiredQuantity = g.Sum(x => x.RequiredBaseQuantity)
                    })
                    .ToList();

                var account = userId.HasValue ? await _repo.GetAccountByIdAsync(userId.Value) : null;
                string snapshotPostedByName = account?.Name ?? "Thu ngân";

                foreach (var item in consolidatedDeductions)
                {
                    var binventory = await _repo.GetBInventoryByBranchAndProductAsync(saleDoc.BranchId, item.ProductId);
                    if (binventory == null) continue;

                    // Kiểm tra xem nguyên liệu này đã được trừ trong chứng từ chưa
                    bool alreadyDeducted = saleDoc.DocumentDetails.Any(d => d.BInventoryId == binventory.Id);
                    if (alreadyDeducted) continue;

                    // Cập nhật tồn kho
                    binventory.Quantity -= item.TotalRequiredQuantity;
                    decimal leftover_old = binventory.LeftOver;
                    decimal val_delta = 0m;
                    if (binventory.Quantity == 0)
                    {
                        val_delta = -((item.TotalRequiredQuantity * binventory.Avg) + leftover_old);
                        binventory.LeftOver = 0;
                    }
                    else
                    {
                        val_delta = -(item.TotalRequiredQuantity * binventory.Avg);
                    }

                    // Ghi thẻ kho InventoryLedger
                    var ledger = new InventoryLedger
                    {
                        BInventoryId = binventory.Id,
                        DocumentId = saleDoc.Id,
                        PostedAt = DateTime.UtcNow,
                        PostedBy = userId ?? 1,
                        DocumentType = DocumentType.Sale,
                        SnapshotPostedByName = snapshotPostedByName,
                        SnapshotBranchName = saleDoc.SnapshotBranchName ?? "",
                        SnapshotProductName = item.Product.Name,
                        SnapshotUnitName = item.Product.Name,
                        ConversionRateSnapshot = 1m,
                        QuantityDelta = -item.TotalRequiredQuantity,
                        InventoryValueDelta = val_delta,
                        RunningQuantity = binventory.Quantity,
                        RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                        RunningAverageCost = binventory.Avg,
                        UnitCost = binventory.Avg,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repo.AddInventoryLedgerAsync(ledger);

                    // Thêm chi tiết chứng từ xuất bán
                    var childDetail = new DocumentDetail
                    {
                        DocumentId = saleDoc.Id,
                        FatherId = null,
                        BInventoryId = binventory.Id,
                        Quantity = item.TotalRequiredQuantity,
                        ConversionRate = 1,
                        BaseQuantity = item.TotalRequiredQuantity,
                        UnitPrice = binventory.Avg,
                        SnapshotProductName = item.Product.Name,
                        SnapshotAvgCost = binventory.Avg
                    };
                    saleDoc.DocumentDetails.Add(childDetail);

                    // Phân bổ Lô hàng theo chuẩn FEFO
                    IReadOnlyList<FefoAllocationResult> fefoAllocations = new List<FefoAllocationResult>();
                    if (_fefoService != null)
                    {
                        try
                        {
                            fefoAllocations = await _fefoService.AllocateAsync(binventory.Id, item.TotalRequiredQuantity);
                        }
                        catch
                        {
                            var batches = await _repo.GetBatchesByInventoryIdAsync(binventory.Id);
                            var activeBatches = batches.Where(b => b.Status == BatchStatus.Active && b.QuantityRemaining > 0)
                                                       .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                                                       .ThenBy(b => b.ExpiryDate)
                                                       .ThenBy(b => b.ReceivedDate)
                                                       .ToList();
                            decimal rem = item.TotalRequiredQuantity;
                            var fallbackList = new List<FefoAllocationResult>();
                            foreach (var b in activeBatches)
                            {
                                if (rem <= 0) break;
                                decimal alloc = Math.Min(b.QuantityRemaining, rem);
                                b.QuantityRemaining -= alloc;
                                if (b.QuantityRemaining == 0) b.Status = BatchStatus.Depleted;
                                b.UpdatedAt = DateTime.UtcNow;
                                fallbackList.Add(new FefoAllocationResult
                                {
                                    BatchId = b.Id,
                                    BatchCode = b.BatchCode,
                                    QuantityAllocated = alloc,
                                    UnitCost = b.UnitCost,
                                    ExpiryDate = b.ExpiryDate,
                                    ReceivedDate = b.ReceivedDate
                                });
                                rem -= alloc;
                            }
                            fefoAllocations = fallbackList;
                        }
                    }

                    if (fefoAllocations.Any())
                    {
                        foreach (var alloc in fefoAllocations)
                        {
                            var batchAlloc = new BatchAllocation
                            {
                                DocumentDetail = childDetail,
                                BatchId = alloc.BatchId,
                                AllocationType = BatchAllocationType.Sale,
                                QuantityAllocated = alloc.QuantityAllocated,
                                UnitCost = alloc.UnitCost,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _repo.AddBatchAllocationAsync(batchAlloc);
                        }
                    }
                }
            }

            // Ghi nhận dòng tiền thanh toán CashFlow
            if (finalAmount > 0 && !saleDoc.CashFlows.Any(cf => cf.Type == MenuGoBE.Models.Enums.CashFlowDetailType.Payment))
            {
                var cashFlow = new CashFlow
                {
                    BranchId = saleDoc.BranchId,
                    DocumentId = saleDoc.Id,
                    PartnerId = saleDoc.PartnerId,
                    Code = "PTBH-" + orderId,
                    BusinessDate = DateTime.UtcNow,
                    Direction = MenuGoBE.Models.Enums.CashFlowDirection.Inflow,
                    Status = MenuGoBE.Models.Enums.CashFlowStatus.Completed,
                    Type = MenuGoBE.Models.Enums.CashFlowDetailType.Payment,
                    PaymentMethod = paymentMethod,
                    TotalAmount = finalAmount,
                    Note = $"Thanh toán đơn {orderId} từ Hóa đơn {saleDoc.Code}",
                    CreatedAt = DateTime.UtcNow
                };
                saleDoc.CashFlows.Add(cashFlow);
            }

            await FreezeDocumentSnapshotsAsync(saleDoc, userId, isSoftDelete: false);
            _repo.Update(saleDoc);

            var returnDoc = await _repo.GetPendingDocumentByOrderIdAsync(orderId, DocumentType.CustomerReturn);
            if (returnDoc != null)
            {
                returnDoc.Status = DocumentStatus.Completed;
                if (partnerId.HasValue) returnDoc.PartnerId = partnerId.Value;
                returnDoc.PostedBy = userId;
                returnDoc.PostedAt = DateTime.UtcNow;

                await FreezeDocumentSnapshotsAsync(returnDoc, userId, isSoftDelete: false);
                _repo.Update(returnDoc);
            }

            await _repo.SaveChangesAsync();
        }
        #endregion
        #endregion

        #region 4. Nghiệp vụ Chứng từ Khách trả hàng (CustomerReturn)

        #region Tạo & Chốt trực tiếp phiếu Khách trả hàng
        public async Task<Models.Document> CreateCustomerReturnCompletedAsync(Models.Document document, long userId)
        {
            await ValidateParentDependencyRuleAsync(DocumentType.CustomerReturn, document.ParentDocumentId);

            document.Type = DocumentType.CustomerReturn;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedBy = userId;
            document.PostedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            if (document.Code.StartsWith("TEMP_"))
            {
                document.Code = $"THK{document.Id:D6}";
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
            return document;
        }
        #endregion

        #region Lấy danh sách phiếu Khách trả hàng
        public async Task<IEnumerable<Models.Document>> GetCustomerReturnListAsync(long branchId)
        {
            return await _repo.GetCustomerReturnDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 5. Nghiệp vụ Chứng từ Chuyển kho (Transfer)

        #region Validation Nghiệp Vụ Phiếu Chuyển Kho (Transfer Validation Rules)
        /// <summary>
        /// Kiểm tra quy tắc bắt buộc cho phiếu Chuyển kho (Transfer):
        /// - Note & DeleteNote <= 255 ký tự
        /// - Chi nhánh gửi (BranchId) và Chi nhánh nhận (ToBranchId) không được rỗng và phải khác nhau.
        /// - Không liên kết Supplier (PartnerId = null).
        /// - Tối thiểu 1 dòng chi tiết (DocumentDetail), không chứa loại ProductType.Processed.
        /// - Số lượng mỗi món: 0.001 <= Quantity <= 10 tỷ.
        /// - Đơn giá mỗi món: 0 <= UnitPrice <= 10 tỷ.
        /// - Tổng giá trị đơn chuyển kho: TotalAmount <= 10 tỷ VNĐ.
        /// </summary>
        private async Task ValidateTransferBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ chuyển kho không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Chi nhánh nhận (ToBranchId) phải khác Chi nhánh gửi (BranchId) và cả hai phải đang hoạt động
            if (!document.ToBranchId.HasValue || document.ToBranchId.Value <= 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferBranchRequired, "Phiếu chuyển kho bắt buộc phải chọn Chi nhánh nhận (ToBranchId).");
            }
            if (document.ToBranchId.Value == document.BranchId)
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferBranchSame, "Chi nhánh nhận không được trùng với Chi nhánh gửi.");
            }

            // Kiểm tra trạng thái hoạt động của Chi nhánh gửi (BranchId)
            var senderBranch = await _repo.GetBranchByIdAsync(document.BranchId);
            if (senderBranch == null || senderBranch.IsDeleted || senderBranch.Status == "Ngừng kinh doanh")
            {
                throw new MenuGoException(ErrorCodes.DocumentBranchInactive, $"Chi nhánh gửi {(senderBranch != null ? $"'{senderBranch.Name}' " : "")}đã ngừng kinh doanh, không thể thực hiện chuyển hàng.");
            }

            // Kiểm tra trạng thái hoạt động của Chi nhánh nhận (ToBranchId)
            var toBranch = await _repo.GetBranchByIdAsync(document.ToBranchId.Value);
            if (toBranch == null || toBranch.IsDeleted || toBranch.Status == "Ngừng kinh doanh")
            {
                throw new MenuGoException(ErrorCodes.DocumentBranchInactive, $"Chi nhánh nhận {(toBranch != null ? $"'{toBranch.Name}' " : "")}đã ngừng kinh doanh, không thể chuyển hàng tới chi nhánh này.");
            }
            #endregion

            #region Quy tắc: Danh sách mặt hàng chuyển kho
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu chuyển kho bắt buộc phải có ít nhất 1 mặt hàng.");
            }

            const decimal MIN_QTY = 0.001m;
            const decimal MAX_QTY = 10_000_000_000m;
            const decimal MAX_PRICE = 10_000_000_000m;
            const decimal MAX_TOTAL = 10_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy mặt hàng BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                if (binventory.Product.Type == ProductType.Processed)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' là loại Chế biến (Processed), không được phép thực hiện chuyển kho.");
                }

                if (detail.Quantity < MIN_QTY || detail.Quantity > MAX_QTY)
                {
                    throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng chuyển kho của mặt hàng '{binventory.Product.Name}' ({detail.Quantity}) không hợp lệ. Phải nằm trong khoảng từ 0.001 đến 10 tỷ.");
                }

                if (detail.UnitPrice < 0 || detail.UnitPrice > MAX_PRICE)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInvalidUnitPrice, $"Đơn giá chuyển của mặt hàng '{binventory.Product.Name}' ({detail.UnitPrice:N0} VNĐ) không hợp lệ. Đơn giá phải từ 0 đến 10 tỷ VNĐ.");
                }

                #region Đơn vị tính và UnitConversion
                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion == null || unitConversion.ProductId != binventory.ProductId)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Đơn vị quy đổi ID {detail.UnitConversionId.Value} không tồn tại hoặc không thuộc về sản phẩm '{binventory.Product.Name}'.");
                    }

                    if (unitConversion.ConversionPoint <= 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Tỷ lệ quy đổi của đơn vị tính ID {detail.UnitConversionId.Value} phải lớn hơn 0.");
                    }

                    detail.ConversionRate = unitConversion.ConversionPoint;
                }
                else
                {
                    detail.ConversionRate = 1;
                }
                #endregion

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
            }
            #endregion

            document.PartnerId = null;

            decimal calculatedTotalAmount = document.DocumentDetails.Sum(d => d.Quantity * d.UnitPrice);
            if (calculatedTotalAmount < 0 || calculatedTotalAmount > MAX_TOTAL)
            {
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit, $"Tổng giá trị phiếu chuyển kho ({calculatedTotalAmount:N0} VNĐ) vượt quá hạn mức tối đa 10 tỷ VNĐ.");
            }

            document.TotalAmount = calculatedTotalAmount;
            document.AmountPaid = 0;
            document.AmountDue = 0;
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Chuyển kho dựa trên ID (Tiền tố CK).
        /// </summary>
        private async Task GenerateOrValidateTransferCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"CK{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"CK{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        #region Xử lý chốt phiếu Chuyển kho tại Chi nhánh Gửi (InTransit)
        /// <summary>
        /// Thực thi chốt phiếu Chuyển kho Completed tại Chi nhánh Gửi:
        /// 1. Kiểm tra tồn kho kho gửi (BInventory). Ném lỗi từ chối nếu không đủ hàng xuất.
        /// 2. Trừ tồn kho kho gửi. Nếu kho gửi về 0 -> Triệt tiêu LeftOver kho gửi.
        /// 3. Ghi thẻ Sổ kho InventoryLedger cho chi nhánh gửi (QuantityDelta âm).
        /// 4. Đổi TransferStatus -> InTransit.
        /// (Quy trình hạch toán tập trung: Không sinh phiếu CashFlow).
        /// </summary>
        private async Task ProcessTransferCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            foreach (var detail in document.DocumentDetails)
            {
                // Kiểm tra tồn kho chi nhánh gửi
                var binventorySend = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventorySend == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho gửi BInventory ID: {detail.BInventoryId}");
                }

                string productName = detail.SnapshotProductName;
                if (string.IsNullOrWhiteSpace(productName) && binventorySend.Product != null)
                {
                    productName = binventorySend.Product.Name;
                }

                if (detail.BaseQuantity > binventorySend.Quantity)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInsufficientInventory, $"Sản phẩm '{productName}' tại chi nhánh gửi không đủ tồn kho để chuyển. Số lượng yêu cầu: {detail.BaseQuantity:N3}, Tồn hiện tại: {binventorySend.Quantity:N3}.");
                }

                // Trừ tồn kho và tính giá trị xuất kho bên gửi
                decimal leftover_old = binventorySend.LeftOver;
                decimal val_delta;

                binventorySend.Quantity -= detail.BaseQuantity;
                detail.SnapshotAvgCost = binventorySend.Avg;

                if (binventorySend.Quantity == 0)
                {
                    val_delta = -((detail.BaseQuantity * binventorySend.Avg) + leftover_old);
                    binventorySend.LeftOver = 0;
                }
                else
                {
                    val_delta = -(detail.BaseQuantity * binventorySend.Avg);
                }

                // Ghi nhận thẻ kho chi nhánh gửi
                var ledgerSend = new InventoryLedger
                {
                    BInventoryId = binventorySend.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Transfer,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = !string.IsNullOrWhiteSpace(productName) ? productName : (binventorySend.Product?.Name ?? string.Empty),
                    SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventorySend.Product?.Name ?? string.Empty),
                    ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                    QuantityDelta = -detail.BaseQuantity,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = binventorySend.Quantity,
                    RunningInventoryValue = binventorySend.Quantity == 0 ? 0m : (binventorySend.Quantity * binventorySend.Avg + binventorySend.LeftOver),
                    RunningAverageCost = binventorySend.Avg,
                    UnitCost = binventorySend.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledgerSend);

                // Phân bổ Lô hàng xuất chuyển theo chuẩn FEFO
                IReadOnlyList<FefoAllocationResult> sendAllocations = new List<FefoAllocationResult>();
                if (_fefoService != null)
                {
                    sendAllocations = await _fefoService.AllocateAsync(binventorySend.Id, detail.BaseQuantity);
                }

                if (sendAllocations.Any())
                {
                    foreach (var alloc in sendAllocations)
                    {
                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetailId = detail.Id,
                            BatchId = alloc.BatchId,
                            AllocationType = BatchAllocationType.TransferOut,
                            QuantityAllocated = alloc.QuantityAllocated,
                            UnitCost = alloc.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                    detail.BatchCodeSnapshot = sendAllocations.First().BatchCode;
                    detail.ExpiryDateSnapshot = sendAllocations.First().ExpiryDate;
                }
            }

            // Chuyển trạng thái sang đang vận chuyển
            document.TransferStatus = DocumentTransferStatus.InTransit;
        }
        #endregion

        #region Xử lý duyệt nhận hoặc từ chối phiếu chuyển kho tại Chi nhánh Nhận
        /// <summary>
        /// Xử lý nhận hàng / từ chối hàng tại Chi nhánh Nhận khi phiếu ở trạng thái InTransit (Hạch toán tập trung):
        /// - Rejected / Cancelled: Từ chối toàn bộ 100%, hoàn trả hàng về kho chi nhánh gửi.
        /// - Received / Completed: Chấp nhận toàn bộ 100%, cộng tồn kho B, bảo toàn giá vốn SnapshotAvgCost từ kho A, tính lại Avg cho kho B, ghi thẻ kho Transfer.
        /// - PartialReceived: Nghiêm cấm nhận một phần.
        /// - Tuyệt đối không sinh CashFlow.
        /// </summary>
        public async Task<Models.Document> ProcessTransferReceiptAsync(long documentId, DocumentTransferStatus status, List<DocumentDetail>? receivedDetails, string? note, long userId)
        {
            // Kiểm tra chứng từ chuyển kho hợp lệ
            var document = await _repo.GetByIdAsync(documentId);
            if (document == null || document.Type != DocumentType.Transfer)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Chuyển kho với ID: {documentId}");
            }

            if (document.Status != DocumentStatus.Completed || document.TransferStatus != DocumentTransferStatus.InTransit)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép xử lý nhận hàng/từ chối khi chứng từ đã được chốt kho gửi và đang ở trạng thái InTransit.");
            }

            if (!document.ToBranchId.HasValue)
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferBranchRequired, "Chứng từ chuyển kho chưa được thiết lập Chi nhánh nhận (ToBranchId).");
            }

            long receivingBranchId = document.ToBranchId.Value;

            // Kiểm tra trạng thái hoạt động của Chi nhánh nhận
            var receivingBranch = await _repo.GetBranchByIdAsync(receivingBranchId);
            if (status != DocumentTransferStatus.Rejected && status != DocumentTransferStatus.Cancelled &&
                (receivingBranch == null || receivingBranch.IsDeleted || receivingBranch.Status == "Ngừng kinh doanh"))
            {
                throw new MenuGoException(ErrorCodes.DocumentBranchInactive, $"Chi nhánh nhận {(receivingBranch != null ? $"'{receivingBranch.Name}' " : "")}đã ngừng kinh doanh, không thể thực hiện nhận hàng vào kho.");
            }

            // Đóng băng thông tin người nhận phía Chi nhánh Nhận
            var receiverAccount = await _repo.GetAccountByIdAsync(userId);
            document.ReceiverAccountId = userId;
            document.SnapshotReceiverName = receiverAccount?.Name ?? string.Empty;
            document.SnapshotReceiverUsername = receiverAccount?.Email ?? string.Empty;
            document.ResponseDate = DateTime.UtcNow;

            // Bắt buộc phải có ghi chú (Note) khi xác nhận nhận hàng hoặc từ chối nhận hàng
            if (string.IsNullOrWhiteSpace(note))
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferReceiptNoteRequired, "Bắt buộc phải nhập Lý do / Ghi chú (Note) khi xác nhận nhận hàng hoặc từ chối nhận hàng.");
            }

            if (status == DocumentTransferStatus.PartialReceived)
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferPartialNotAllowed, "Không được phép nhận một phần. Vui lòng chọn Chấp nhận toàn bộ hoặc Từ chối nhận.");
            }

            if (status == DocumentTransferStatus.Rejected || status == DocumentTransferStatus.Cancelled)
            {
                // Ghi nhận lý do từ chối
                document.Note = string.IsNullOrWhiteSpace(document.Note) ? $"[TỪ CHỐI]: {note}" : $"{document.Note} | [TỪ CHỐI]: {note}";

                // Hoàn trả 100% hàng về kho chi nhánh gửi
                foreach (var detail in document.DocumentDetails)
                {
                    var binventorySend = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                    if (binventorySend == null) continue;

                    string productName = detail.SnapshotProductName;
                    if (string.IsNullOrWhiteSpace(productName) && binventorySend.Product != null)
                    {
                        productName = binventorySend.Product.Name;
                    }

                    decimal q_old = binventorySend.Quantity;
                    decimal avg_old = binventorySend.Avg;
                    decimal leftover_old = binventorySend.LeftOver;
                    decimal ret_cost = detail.SnapshotAvgCost > 0 ? detail.SnapshotAvgCost : (binventorySend.Avg > 0 ? binventorySend.Avg : (detail.UnitPrice > 0 ? detail.UnitPrice : 1m));
                    decimal ret_value = detail.BaseQuantity * ret_cost;

                    decimal b1 = (q_old * avg_old) + leftover_old + ret_value;
                    decimal b2 = q_old + detail.BaseQuantity;
                    decimal avg_new = (b2 > 0) ? Math.Round(b1 / b2, 6) : 0m;
                    decimal c1 = avg_new * b2;
                    decimal leftover_new = b1 - c1;

                    binventorySend.Avg = avg_new;
                    binventorySend.LeftOver = leftover_new;
                    binventorySend.Quantity = b2;

                    var ledgerReturn = new InventoryLedger
                    {
                        BInventoryId = binventorySend.Id,
                        DocumentId = document.Id,
                        PostedAt = DateTime.UtcNow,
                        PostedBy = userId,
                        DocumentType = DocumentType.Transfer,
                        SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                        SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                        SnapshotProductName = !string.IsNullOrWhiteSpace(productName) ? productName : (binventorySend.Product?.Name ?? string.Empty),
                        SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventorySend.Product?.Name ?? string.Empty),
                        ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                        QuantityDelta = +detail.BaseQuantity,
                        InventoryValueDelta = +ret_value,
                        RunningQuantity = b2,
                        RunningInventoryValue = c1,
                        RunningAverageCost = avg_new,
                        UnitCost = ret_cost,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.AddInventoryLedgerAsync(ledgerReturn);

                    // Hoàn trả chính xác các Lô nguồn đã xuất trong Transfer Send
                    var allocations = (await _repo.GetBatchAllocationsByDetailIdsAsync(new[] { detail.Id })) ?? new List<BatchAllocation>();
                    foreach (var alloc in allocations.Where(a => a.AllocationType == BatchAllocationType.TransferOut))
                    {
                        if (alloc.Batch != null)
                        {
                            alloc.Batch.QuantityRemaining += alloc.QuantityAllocated;
                            if (alloc.Batch.Status == BatchStatus.Depleted)
                            {
                                alloc.Batch.Status = BatchStatus.Active;
                            }
                            alloc.Batch.UpdatedAt = DateTime.UtcNow;

                            var rejectAlloc = new BatchAllocation
                            {
                                DocumentDetailId = detail.Id,
                                BatchId = alloc.BatchId,
                                AllocationType = BatchAllocationType.TransferReturn,
                                QuantityAllocated = alloc.QuantityAllocated,
                                UnitCost = alloc.UnitCost,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _repo.AddBatchAllocationAsync(rejectAlloc);
                        }
                    }
                }

                document.TransferStatus = DocumentTransferStatus.Rejected;
                document.Status = DocumentStatus.Cancelled;
            }
            else if (status == DocumentTransferStatus.Received || status == DocumentTransferStatus.Completed)
            {
                // Chấp nhận toàn bộ: Tăng tồn kho và bảo toàn giá vốn COGS cho kho chi nhánh nhận
                foreach (var detail in document.DocumentDetails)
                {
                    // Lấy thông tin kho gửi để truy xuất ProductId
                    var binventorySend = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                    if (binventorySend == null) continue;

                    // Tự động gán ReceivedQuantity = 100% Quantity đã gửi
                    detail.ReceivedQuantity = detail.Quantity;

                    // Tìm hoặc tạo BInventory tại chi nhánh nhận
                    var binventoryRec = await _repo.GetBInventoryByBranchAndProductAsync(receivingBranchId, binventorySend.ProductId);
                    if (binventoryRec == null)
                    {
                        binventoryRec = new BInventory
                        {
                            BranchId = receivingBranchId,
                            ProductId = binventorySend.ProductId,
                            Quantity = 0,
                            Avg = 0,
                            LeftOver = 0
                        };
                        await _repo.AddBInventoryAsync(binventoryRec);
                        await _repo.SaveChangesAsync();
                    }

                    // Đơn giá vốn chuyển kho: Lấy đúng SnapshotAvgCost từ kho gửi (tuyệt đối không 0đ)
                    decimal cogsUnitCost = detail.SnapshotAvgCost > 0 
                        ? detail.SnapshotAvgCost 
                        : (binventorySend.Avg > 0 ? binventorySend.Avg : (detail.UnitPrice > 0 ? detail.UnitPrice : 1m));
                    decimal importValue = detail.BaseQuantity * cogsUnitCost;

                    // Cập nhật giá vốn bình quân gia quyền tại kho nhận B
                    decimal q_old = binventoryRec.Quantity;
                    decimal avg_old = binventoryRec.Avg;
                    decimal leftover_old = binventoryRec.LeftOver;

                    decimal b1 = (q_old * avg_old) + leftover_old + importValue;
                    decimal b2 = q_old + detail.BaseQuantity;
                    decimal avg_new = (b2 > 0) ? Math.Round(b1 / b2, 6) : cogsUnitCost;
                    decimal c1 = avg_new * b2;
                    decimal leftover_new = b1 - c1;

                    binventoryRec.Quantity = b2;
                    binventoryRec.Avg = avg_new;
                    binventoryRec.LeftOver = leftover_new;

                    // Ghi thẻ Sổ kho InventoryLedger (Transfer In) cho chi nhánh nhận
                    var ledgerRec = new InventoryLedger
                    {
                        BInventoryId = binventoryRec.Id,
                        DocumentId = document.Id,
                        PostedAt = DateTime.UtcNow,
                        PostedBy = userId,
                        DocumentType = DocumentType.Transfer,
                        SnapshotPostedByName = document.SnapshotReceiverName ?? receiverAccount?.Name ?? string.Empty,
                        SnapshotBranchName = receivingBranch?.Name ?? string.Empty,
                        SnapshotProductName = !string.IsNullOrWhiteSpace(detail.SnapshotProductName) ? detail.SnapshotProductName : (binventorySend.Product?.Name ?? string.Empty),
                        SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : string.Empty,
                        ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                        QuantityDelta = +detail.BaseQuantity,
                        InventoryValueDelta = +importValue,
                        RunningQuantity = b2,
                        RunningInventoryValue = c1,
                        RunningAverageCost = avg_new,
                        UnitCost = cogsUnitCost,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.AddInventoryLedgerAsync(ledgerRec);
                }

                // Cập nhật ghi chú và trạng thái phiếu
                if (!string.IsNullOrWhiteSpace(note))
                {
                    document.Note = string.IsNullOrWhiteSpace(document.Note) ? note : $"{document.Note} | {note}";
                }

                document.TransferStatus = DocumentTransferStatus.Received;
                document.Status = DocumentStatus.Completed;
            }
            else
            {
                throw new MenuGoException(ErrorCodes.DocumentTransferReceiptStatusInvalid, "Trạng thái nhận hàng không hợp lệ. Chỉ chấp nhận Received (Chấp nhận) hoặc Rejected (Từ chối).");
            }

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion
        #endregion

        #region API Handlers cho Transfer
        public async Task<Models.Document> CreateTransferPendingAsync(Models.Document document, long userId)
        {
            await ValidateTransferBaseRulesAsync(document);
            var created = await CreatePendingBaseAsync(document, DocumentType.Transfer, userId);
            await GenerateOrValidateTransferCodeAsync(created);
            return created;
        }

        public async Task<Models.Document> CreateTransferCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.Transfer;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateTransferBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateTransferCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessTransferCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<Models.Document> UpdateTransferPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.Transfer);
            await ValidateTransferBaseRulesAsync(existing);
            await GenerateOrValidateTransferCodeAsync(existing);
            return existing;
        }

        public async Task<Models.Document> CompleteTransferAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Transfer)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Chuyển kho với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chốt chứng từ khi ở trạng thái Pending.");
            }

            await ValidateTransferBaseRulesAsync(existing);
            await GenerateOrValidateTransferCodeAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessTransferCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteTransferPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.Transfer, userId, deleteNote);
        }

        public async Task<IEnumerable<Models.Document>> GetTransferListAsync(long branchId)
        {
            return await _repo.GetTransferDocumentsAsync(branchId);
        }
        #endregion

        #region 6. Nghiệp vụ Chứng từ Xuất dùng nội bộ (Export)

        #region Tạo phiếu Xuất dùng nội bộ ở trạng thái Pending
        public async Task<Models.Document> CreateExportPendingAsync(Models.Document document, long userId)
        {
            return await CreatePendingBaseAsync(document, DocumentType.Export, userId);
        }
        #endregion

        #region Cập nhật phiếu Xuất dùng nội bộ Pending
        public async Task<Models.Document> UpdateExportPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            return await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.Export);
        }
        #endregion

        #region Tạo phiếu Xuất dùng nội bộ ở trạng thái Completed trực tiếp
        public async Task<Models.Document> CreateExportCompletedAsync(Models.Document document, long userId)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ xuất dùng nội bộ không được để trống.");
            }

            document.Type = DocumentType.Export;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateExportDeleteBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateExportDeleteCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessExportDeleteCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }
        #endregion

        #region Chốt phiếu Xuất dùng nội bộ sang Completed
        public async Task<Models.Document> CompleteExportAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Export)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Xuất dùng nội bộ với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await ValidateExportDeleteBaseRulesAsync(existing);
            await GenerateOrValidateExportDeleteCodeAsync(existing);
            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessExportDeleteCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }
        #endregion

        #region Xóa mềm phiếu Xuất dùng nội bộ Pending
        public async Task<bool> SoftDeleteExportPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.Export, userId, deleteNote);
        }
        #endregion

        #region Lấy danh sách phiếu Xuất dùng nội bộ
        public async Task<IEnumerable<Models.Document>> GetExportListAsync(long branchId)
        {
            return await _repo.GetExportDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 6b. Nghiệp vụ Chứng từ Xuất hủy (ExportDelete)

        #region Validation Nghiệp Vụ Phiếu Xuất Hủy (ExportDelete Validation Rules)
        /// <summary>
        /// Kiểm tra các quy tắc bắt buộc chung cho phiếu Xuất hủy (ExportDelete):
        /// - Note & DeleteNote <= 255 ký tự
        /// - Tối thiểu 1 chi tiết (DocumentDetail), không trùng BInventoryId
        /// - Product.Type != ProductType.Processed (không được phép chọn loại Processed)
        /// - Số lượng 0.001 <= Quantity <= 10 tỷ (không cho phép xuất âm hoặc 0)
        /// - Không liên kết Supplier (PartnerId = null)
        /// </summary>
        private async Task ValidateExportDeleteBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ xuất hủy không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Tối thiểu 1 DocumentDetail
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu xuất hủy bắt buộc phải có tối thiểu 1 mặt hàng chi tiết (DocumentDetail).");
            }
            #endregion

            #region Quy tắc: Mỗi BInventoryId chỉ xuất hiện 1 lần
            var duplicateBInventory = document.DocumentDetails
                .GroupBy(d => d.BInventoryId)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateBInventory != null)
            {
                throw new MenuGoException(ErrorCodes.DocumentDuplicateDetail, $"Mặt hàng BInventory ID {duplicateBInventory.Key} bị trùng lặp trong phiếu xuất hủy. Mỗi mặt hàng chỉ được xuất hiện 1 lần.");
            }
            #endregion

            #region Quy tắc: Kiểm tra loại sản phẩm (!= Processed) và Số lượng (không âm/không 0)
            const decimal MIN_QTY = 0.001m;
            const decimal MAX_QTY = 10_000_000_000m;
            const decimal MAX_AMOUNT = 10_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy mặt hàng BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                var productType = binventory.Product.Type;
                if (productType == ProductType.Processed)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' thuộc loại Processed (thành phẩm pha chế/chế biến) không được phép xuất hủy. Dạng phiếu này chỉ chấp nhận các loại sản phẩm ngoại trừ Processed.");
                }

                if (detail.Quantity < MIN_QTY || detail.Quantity > MAX_QTY)
                {
                    throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng xuất hủy của mặt hàng '{binventory.Product.Name}' ({detail.Quantity}) không hợp lệ. Phải nằm trong khoảng từ 0.001 đến 10 tỷ (không cho phép số âm hoặc 0).");
                }

                #region Đơn vị tính và UnitConversion
                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion == null || unitConversion.ProductId != binventory.ProductId)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Đơn vị quy đổi ID {detail.UnitConversionId.Value} không tồn tại hoặc không thuộc về sản phẩm '{binventory.Product.Name}'.");
                    }

                    if (unitConversion.ConversionPoint <= 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Tỷ lệ quy đổi của đơn vị tính ID {detail.UnitConversionId.Value} phải lớn hơn 0.");
                    }

                    detail.ConversionRate = unitConversion.ConversionPoint;
                }
                else
                {
                    detail.ConversionRate = 1;
                }
                #endregion

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
                decimal avgCost = binventory.Avg > 0 
                    ? binventory.Avg 
                    : (detail.UnitPrice > 0 && detail.ConversionRate > 0 
                        ? detail.UnitPrice / detail.ConversionRate 
                        : 0m);
                detail.SnapshotAvgCost = avgCost;
                detail.UnitPrice = avgCost * detail.ConversionRate;
            }
            #endregion

            // Xuất hủy không liên kết tới Supplier (PartnerId = null)
            document.PartnerId = null;

            decimal calculatedTotalAmount = document.DocumentDetails.Sum(d => d.BaseQuantity * d.SnapshotAvgCost);
            if (calculatedTotalAmount < 0 || calculatedTotalAmount > MAX_AMOUNT)
            {
                throw new MenuGoException(ErrorCodes.DocumentTotalAmountExceedsLimit, $"Tổng giá trị phiếu xuất hủy ({calculatedTotalAmount:N0} VNĐ) vượt quá 10 tỷ VNĐ.");
            }
            document.TotalAmount = calculatedTotalAmount;
            document.AmountPaid = 0;
            document.AmountDue = 0;
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Xuất hủy dựa trên ID (Tiền tố XH).
        /// </summary>
        private async Task GenerateOrValidateExportDeleteCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"XH{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"XH{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Thực thi chốt phiếu Xuất hủy Completed:
        /// - Kiểm tra tồn kho thực tế: detail.BaseQuantity <= binventory.Quantity (nếu quá -> throw InvalidOperationException)
        /// - Trừ tồn kho: binventory.Quantity -= detail.BaseQuantity
        /// - Nếu kho về 0 -> Triệt tiêu toàn bộ LeftOver cũ: binventory.LeftOver = 0
        /// - Ghi Sổ kho InventoryLedger (QuantityDelta âm)
        /// </summary>
        private async Task ProcessExportDeleteCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng BInventory ID: {detail.BInventoryId}");
                }

                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion != null && unitConversion.ConversionPoint > 0)
                    {
                        detail.ConversionRate = unitConversion.ConversionPoint;
                    }
                }
                if (detail.ConversionRate <= 0) detail.ConversionRate = 1;

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
                detail.SnapshotAvgCost = binventory.Avg;
                detail.UnitPrice = binventory.Avg * detail.ConversionRate;

                decimal q_export = detail.BaseQuantity;
                if (q_export > binventory.Quantity)
                {
                    string productName = binventory.Product?.Name ?? $"BInventory ID {binventory.Id}";
                    throw new MenuGoException(ErrorCodes.DocumentInsufficientInventory, $"Số lượng xuất hủy của mặt hàng '{productName}' ({q_export}) vượt quá số lượng tồn kho hiện tại ({binventory.Quantity}). Không cho phép xuất vượt quá tồn kho hoặc xuất âm.");
                }

                decimal leftover_old = binventory.LeftOver;
                decimal val_delta;

                // Trừ tồn kho
                binventory.Quantity -= q_export;

                if (binventory.Quantity == 0)
                {
                    // Khi tồn kho về 0 -> Triệt tiêu toàn bộ LeftOver cũ
                    val_delta = -((q_export * binventory.Avg) + leftover_old);
                    binventory.LeftOver = 0;
                }
                else
                {
                    val_delta = -(q_export * binventory.Avg);
                }

                // Bản ghi Sổ kho InventoryLedger
                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = document.Type,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = !string.IsNullOrWhiteSpace(detail.SnapshotProductName) ? detail.SnapshotProductName : (binventory.Product?.Name ?? string.Empty),
                    SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventory.Product?.Name ?? string.Empty),
                    ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                    QuantityDelta = -q_export,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = binventory.Quantity,
                    RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                    RunningAverageCost = binventory.Avg,
                    UnitCost = binventory.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);

                // Xử lý phân bổ Lô xuất hủy/xuất dùng (Chỉ định Lô cụ thể hoặc FEFO tự động)
                if (!string.IsNullOrWhiteSpace(detail.BatchCodeSnapshot))
                {
                    var explicitBatch = await _repo.GetBatchByInventoryAndCodeAsync(binventory.Id, detail.BatchCodeSnapshot);
                    if (explicitBatch != null)
                    {
                        if (q_export > explicitBatch.QuantityRemaining)
                        {
                            throw new MenuGoException(ErrorCodes.DocumentInsufficientInventory, $"Số lượng xuất ({q_export:N3}) vượt quá số lượng còn lại của Lô được chỉ định '{explicitBatch.BatchCode}' ({explicitBatch.QuantityRemaining:N3}).");
                        }

                        explicitBatch.QuantityRemaining -= q_export;
                        if (explicitBatch.QuantityRemaining == 0)
                        {
                            explicitBatch.Status = BatchStatus.Depleted;
                        }
                        explicitBatch.UpdatedAt = DateTime.UtcNow;

                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetailId = detail.Id,
                            BatchId = explicitBatch.Id,
                            AllocationType = BatchAllocationType.Disposal,
                            QuantityAllocated = q_export,
                            UnitCost = explicitBatch.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
                else
                {
                    // Tự động phân bổ Lô xuất hủy ưu tiên Lô hết hạn và Lô cũ nhất trước
                    var batches = (await _repo.GetBatchesByInventoryIdAsync(binventory.Id)) ?? new List<BInventoryBatch>();
                    var candidateBatches = batches
                        .Where(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted)
                        .OrderBy(b => b.ExpiryDate == null ? 1 : 0)
                        .ThenBy(b => b.ExpiryDate)
                        .ThenBy(b => b.ReceivedDate)
                        .ToList();

                    decimal remainingToAllocate = q_export;
                    foreach (var b in candidateBatches)
                    {
                        if (remainingToAllocate <= 0) break;
                        decimal allocateQty = Math.Min(b.QuantityRemaining, remainingToAllocate);

                        b.QuantityRemaining -= allocateQty;
                        if (b.QuantityRemaining == 0)
                        {
                            b.Status = BatchStatus.Depleted;
                        }
                        b.UpdatedAt = DateTime.UtcNow;

                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetailId = detail.Id,
                            BatchId = b.Id,
                            AllocationType = BatchAllocationType.Disposal,
                            QuantityAllocated = allocateQty,
                            UnitCost = b.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);

                        remainingToAllocate -= allocateQty;
                    }
                }
            }

            document.TotalAmount = document.DocumentDetails.Sum(d => d.BaseQuantity * d.SnapshotAvgCost);
        }
        #endregion

        #region API Handlers cho ExportDelete
        public async Task<Models.Document> CreateExportDeletePendingAsync(Models.Document document, long userId)
        {
            await ValidateExportDeleteBaseRulesAsync(document);
            var created = await CreatePendingBaseAsync(document, DocumentType.ExportDelete, userId);
            await GenerateOrValidateExportDeleteCodeAsync(created);
            return created;
        }

        public async Task<Models.Document> CreateExportDeleteCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.ExportDelete;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateExportDeleteBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateExportDeleteCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessExportDeleteCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<Models.Document> UpdateExportDeletePendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.ExportDelete);
            await ValidateExportDeleteBaseRulesAsync(existing);
            await GenerateOrValidateExportDeleteCodeAsync(existing);
            return existing;
        }

        public async Task<Models.Document> CompleteExportDeleteAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.ExportDelete)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Xuất hủy với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chốt chứng từ khi ở trạng thái Pending.");
            }

            await ValidateExportDeleteBaseRulesAsync(existing);
            await GenerateOrValidateExportDeleteCodeAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessExportDeleteCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteExportDeletePendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.ExportDelete, userId, deleteNote);
        }

        public async Task<IEnumerable<Models.Document>> GetExportDeleteListAsync(long branchId)
        {
            return await _repo.GetExportDeleteDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 7. Nghiệp vụ Chứng từ Sản xuất (Production)

        #region Validation Nghiệp Vụ Phiếu Sản Xuất (Production Validation Rules cho Pending & Base)
        /// <summary>
        /// Kiểm tra quy tắc cơ bản khi lưu tạm (Pending) cho phiếu Sản xuất (Production):
        /// - Note & DeleteNote <= 255 ký tự
        /// - Chứa duy nhất 1 sản phẩm sản xuất chính (thành phẩm Manufactured với FatherId == null)
        /// - Bắt buộc thành phẩm phải có công thức chi tiết (RecipesDetailed). Nếu không có -> Reject.
        /// - Số lượng thành phẩm > 0 và <= 10 tỷ (luôn tính theo đơn vị nhỏ nhất: ConversionRate = 1)
        /// - Không kiểm tra tồn kho NVL và không sinh các dòng chi tiết NVL phụ khi Pending.
        /// </summary>
        private async Task ValidateProductionBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ sản xuất không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Phải có ít nhất 1 DocumentDetail cho thành phẩm chính
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentProductionInfoRequired, "Phiếu sản xuất bắt buộc phải có thông tin sản phẩm sản xuất.");
            }

            // Tìm dòng sản phẩm chính (FatherId == null)
            var parentDetail = document.DocumentDetails.FirstOrDefault(d => d.FatherId == null);
            if (parentDetail == null)
            {
                parentDetail = document.DocumentDetails.First();
            }

            var binventory = await _repo.GetBInventoryByIdAsync(parentDetail.BInventoryId);
            if (binventory == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng BInventory ID: {parentDetail.BInventoryId}");
            }

            if (binventory.Product == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {parentDetail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
            }

            if (binventory.Product.Type != ProductType.Manufactured)
            {
                throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' có loại (ProductType = {binventory.Product.Type}) không phải là sản phẩm sản xuất (Manufactured). Phiếu sản xuất chỉ áp dụng cho sản phẩm loại Manufactured.");
            }

            // Kiểm tra công thức chi tiết của thành phẩm
            var recipeItems = await _repo.GetRecipesByParentProductIdAsync(binventory.ProductId);
            if (recipeItems == null || recipeItems.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentRecipeNotFound, $"Sản phẩm sản xuất '{binventory.Product.Name}' chưa có công thức chi tiết (Recipe). Không thể lập phiếu sản xuất.");
            }

            const decimal MIN_QTY = 0.001m;
            const decimal MAX_QTY = 10_000_000_000m;
            if (parentDetail.Quantity < MIN_QTY || parentDetail.Quantity > MAX_QTY)
            {
                throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng sản xuất của sản phẩm '{binventory.Product.Name}' ({parentDetail.Quantity}) không hợp lệ. Số lượng phải lớn hơn 0 và từ 0.001 đến 10 tỷ.");
            }

            // Luôn dùng đơn vị kho cơ sở nhỏ nhất (ConversionRate = 1)
            parentDetail.UnitConversionId = null;
            parentDetail.ConversionRate = 1;
            parentDetail.BaseQuantity = parentDetail.Quantity;

            // Kiểm tra bắt buộc tồn kho nguyên liệu NVL tại chi nhánh
            foreach (var recipeItem in recipeItems)
            {
                decimal requiredQty = parentDetail.BaseQuantity * recipeItem.Quantity;
                var ingredientBinventory = await _repo.GetBInventoryByBranchAndProductAsync(document.BranchId, recipeItem.IngredientProductId);

                if (ingredientBinventory == null)
                {
                    string ingredientName = recipeItem.IngredientProduct?.Name ?? $"ProductId {recipeItem.IngredientProductId}";
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng của nguyên liệu '{ingredientName}' tại chi nhánh để sản xuất.");
                }

                if (requiredQty > ingredientBinventory.Quantity)
                {
                    string ingredientName = ingredientBinventory.Product?.Name ?? recipeItem.IngredientProduct?.Name ?? $"ProductId {recipeItem.IngredientProductId}";
                    throw new MenuGoException(ErrorCodes.DocumentProductionInsufficientRawMaterial, $"Nguyên liệu '{ingredientName}' không đủ tồn kho để sản xuất. Số lượng cần: {requiredQty:N3}, Tồn kho hiện tại: {ingredientBinventory.Quantity:N3}.");
                }
            }
            #endregion

            document.PartnerId = null;
            document.TotalAmount = 0;
            document.AmountPaid = 0;
            document.AmountDue = 0;
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Sản xuất dựa trên ID (Tiền tố SX).
        /// </summary>
        private async Task GenerateOrValidateProductionCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"SX{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"SX{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Thực thi chốt phiếu Sản xuất Completed:
        /// 1. Kiểm tra tồn kho nguyên liệu NVL. Từ chối ngay nếu có 1 NVL không đủ tồn kho.
        /// 2. Tự động sinh/cập nhật các dòng DocumentDetail phụ cho NVL với FatherId trỏ về thành phẩm.
        /// 3. Trừ tồn kho NVL, ghi nhận thẻ Sổ kho InventoryLedger cho từng NVL (số lượng âm). Nếu tồn NVL về 0 -> reset LeftOver = 0.
        /// 4. Tính tổng chi phí NVL tiêu hao -> Cộng tồn kho Thành phẩm TP, tính giá vốn trung bình Avg & LeftOver mới cho TP, ghi thẻ Sổ kho InventoryLedger cho TP (số lượng dương).
        /// </summary>
        private async Task ProcessProductionCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            var parentDetail = document.DocumentDetails.FirstOrDefault(d => d.FatherId == null) ?? document.DocumentDetails.First();
            var parentBinventory = await _repo.GetBInventoryByIdAsync(parentDetail.BInventoryId);
            if (parentBinventory == null || parentBinventory.Product == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho thành phẩm BInventory ID: {parentDetail.BInventoryId}");
            }

            // Lấy danh sách công thức chi tiết
            var recipeItems = await _repo.GetRecipesByParentProductIdAsync(parentBinventory.ProductId);
            if (recipeItems == null || recipeItems.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentRecipeNotFound, $"Sản phẩm sản xuất '{parentBinventory.Product.Name}' chưa có công thức chi tiết (Recipe).");
            }

            decimal producedQty = parentDetail.BaseQuantity;

            // Bước 1: Kiểm tra đủ tồn kho từng nguyên liệu
            var ingredientRequirements = new List<(RecipesDetailed RecipeItem, BInventory IngredientBinventory, decimal RequiredQty)>();
            foreach (var recipeItem in recipeItems)
            {
                decimal requiredQty = producedQty * recipeItem.Quantity;
                var ingredientBinventory = await _repo.GetBInventoryByBranchAndProductAsync(document.BranchId, recipeItem.IngredientProductId);

                if (ingredientBinventory == null)
                {
                    string ingredientName = recipeItem.IngredientProduct?.Name ?? $"ProductId {recipeItem.IngredientProductId}";
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng của nguyên liệu '{ingredientName}' tại chi nhánh.");
                }

                if (requiredQty > ingredientBinventory.Quantity)
                {
                    string ingredientName = ingredientBinventory.Product?.Name ?? recipeItem.IngredientProduct?.Name ?? $"ProductId {recipeItem.IngredientProductId}";
                    throw new MenuGoException(ErrorCodes.DocumentProductionInsufficientRawMaterial, $"Nguyên liệu '{ingredientName}' không đủ tồn kho để sản xuất. Số lượng cần: {requiredQty:N3}, Tồn kho hiện tại: {ingredientBinventory.Quantity:N3}.");
                }

                ingredientRequirements.Add((recipeItem, ingredientBinventory, requiredQty));
            }

            // Xóa các dòng chi tiết NVL cũ (nếu có) để tạo lại chuẩn từ công thức
            var oldChildDetails = document.DocumentDetails.Where(d => d.FatherId != null).ToList();
            foreach (var child in oldChildDetails)
            {
                document.DocumentDetails.Remove(child);
            }

            decimal totalProductionCost = 0m;

            // Bước 2 & 3: Xử lý trừ kho NVL và ghi Sổ kho NVL
            foreach (var (recipeItem, ingredientBinventory, requiredQty) in ingredientRequirements)
            {
                string ingredientName = ingredientBinventory.Product?.Name ?? recipeItem.IngredientProduct?.Name ?? string.Empty;

                decimal leftover_old = ingredientBinventory.LeftOver;
                decimal val_delta;

                // Trừ tồn kho NVL
                ingredientBinventory.Quantity -= requiredQty;
                decimal ingredientCost = requiredQty * ingredientBinventory.Avg;
                totalProductionCost += ingredientCost;

                if (ingredientBinventory.Quantity == 0)
                {
                    // Nếu NVL về 0 -> Triệt tiêu LeftOver cũ
                    val_delta = -((requiredQty * ingredientBinventory.Avg) + leftover_old);
                    ingredientBinventory.LeftOver = 0;
                }
                else
                {
                    val_delta = -ingredientCost;
                }

                // Bản ghi Sổ kho InventoryLedger cho NVL bị dùng
                var ingredientLedger = new InventoryLedger
                {
                    BInventoryId = ingredientBinventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Production,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = ingredientName,
                    SnapshotUnitName = ingredientName,
                    ConversionRateSnapshot = 1m,
                    QuantityDelta = -requiredQty,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = ingredientBinventory.Quantity,
                    RunningInventoryValue = ingredientBinventory.Quantity == 0 ? 0m : (ingredientBinventory.Quantity * ingredientBinventory.Avg + ingredientBinventory.LeftOver),
                    RunningAverageCost = ingredientBinventory.Avg,
                    UnitCost = ingredientBinventory.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ingredientLedger);

                // Tạo dòng chi tiết NVL phụ
                var childDetail = new DocumentDetail
                {
                    DocumentId = document.Id,
                    FatherId = parentDetail.Id,
                    BInventoryId = ingredientBinventory.Id,
                    Quantity = requiredQty,
                    ConversionRate = 1,
                    BaseQuantity = requiredQty,
                    UnitPrice = ingredientBinventory.Avg,
                    SnapshotProductName = ingredientName,
                    SnapshotAvgCost = ingredientBinventory.Avg
                };
                document.DocumentDetails.Add(childDetail);

                // Phân bổ Lô NVL theo chuẩn FEFO
                if (_fefoService != null)
                {
                    var ingAllocations = await _fefoService.AllocateAsync(ingredientBinventory.Id, requiredQty);
                    foreach (var alloc in ingAllocations)
                    {
                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetail = childDetail,
                            BatchId = alloc.BatchId,
                            AllocationType = BatchAllocationType.ProductionConsumption,
                            QuantityAllocated = alloc.QuantityAllocated,
                            UnitCost = alloc.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
            }

            // Bước 4: Xử lý cộng kho Thành phẩm TP & Tính giá vốn trung bình TP
            decimal q_old = parentBinventory.Quantity;
            decimal avg_old = parentBinventory.Avg;
            decimal tp_leftover_old = parentBinventory.LeftOver;

            decimal b1 = (q_old * avg_old) + tp_leftover_old + totalProductionCost;
            decimal b2 = q_old + producedQty;

            decimal tp_avg = (b2 > 0) ? Math.Round(b1 / b2, 6) : 0m;
            decimal c1 = tp_avg * b2;
            decimal tp_leftover_new = b1 - c1;

            parentBinventory.Avg = tp_avg;
            parentBinventory.LeftOver = tp_leftover_new;
            parentBinventory.Quantity = b2;

            decimal tpUnitCost = (producedQty > 0) ? (totalProductionCost / producedQty) : tp_avg;
            parentDetail.SnapshotAvgCost = tp_avg;
            parentDetail.UnitPrice = tpUnitCost;

            // Gán giá trị phiếu Sản xuất bằng đúng tổng chi phí sản xuất thành phẩm
            document.TotalAmount = totalProductionCost;

            // Bản ghi Sổ kho InventoryLedger cho Thành phẩm sản xuất
            var parentLedger = new InventoryLedger
            {
                BInventoryId = parentBinventory.Id,
                DocumentId = document.Id,
                PostedAt = document.PostedAt ?? DateTime.UtcNow,
                PostedBy = userId,
                DocumentType = DocumentType.Production,
                SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                SnapshotProductName = !string.IsNullOrWhiteSpace(parentDetail.SnapshotProductName) ? parentDetail.SnapshotProductName : (parentBinventory.Product?.Name ?? string.Empty),
                SnapshotUnitName = !string.IsNullOrWhiteSpace(parentDetail.SnapshotUnitName) ? parentDetail.SnapshotUnitName : (parentBinventory.Product?.Name ?? string.Empty),
                ConversionRateSnapshot = parentDetail.ConversionRate > 0 ? parentDetail.ConversionRate : 1m,
                QuantityDelta = +producedQty,
                InventoryValueDelta = +totalProductionCost,
                RunningQuantity = b2,
                RunningInventoryValue = c1,
                RunningAverageCost = tp_avg,
                UnitCost = tpUnitCost,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddInventoryLedgerAsync(parentLedger);

            // Bước 5: Tạo Lô hàng mới (BInventoryBatch) cho thành phẩm sản xuất
            string prdBatchCode = !string.IsNullOrWhiteSpace(parentDetail.BatchCodeSnapshot)
                ? parentDetail.BatchCodeSnapshot
                : $"LOT-PRD-{document.Code}-{parentDetail.Id}";

            DateTime manufactureDate = parentDetail.ManufactureDateSnapshot ?? (document.OrderDate != default ? document.OrderDate : DateTime.UtcNow);

            DateTime? expiryDate = parentDetail.ExpiryDateSnapshot.HasValue
                ? parentDetail.ExpiryDateSnapshot.Value
                : (parentBinventory.Product.ShelfLifeDays.HasValue
                    ? manufactureDate.AddDays(parentBinventory.Product.ShelfLifeDays.Value)
                    : null);

            parentDetail.BatchCodeSnapshot = prdBatchCode;
            parentDetail.ManufactureDateSnapshot = manufactureDate;
            parentDetail.ExpiryDateSnapshot = expiryDate;

            var existingPrdBatch = await _repo.GetBatchByInventoryAndCodeAsync(parentBinventory.Id, prdBatchCode);
            if (existingPrdBatch != null)
            {
                existingPrdBatch.QuantityOriginal += producedQty;
                existingPrdBatch.QuantityRemaining += producedQty;
                if (existingPrdBatch.Status == BatchStatus.Depleted)
                {
                    existingPrdBatch.Status = BatchStatus.Active;
                }
                existingPrdBatch.ManufactureDate = manufactureDate;
                if (expiryDate.HasValue) existingPrdBatch.ExpiryDate = expiryDate;
                existingPrdBatch.UpdatedAt = DateTime.UtcNow;

                var prdAlloc = new BatchAllocation
                {
                    DocumentDetail = parentDetail,
                    Batch = existingPrdBatch,
                    AllocationType = BatchAllocationType.ProductionReceipt,
                    QuantityAllocated = producedQty,
                    UnitCost = tpUnitCost,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddBatchAllocationAsync(prdAlloc);
            }
            else
            {
                var newPrdBatch = new BInventoryBatch
                {
                    BInventoryId = parentBinventory.Id,
                    BatchCode = prdBatchCode,
                    QuantityOriginal = producedQty,
                    QuantityRemaining = producedQty,
                    UnitCost = tpUnitCost,
                    ManufactureDate = manufactureDate,
                    ExpiryDate = expiryDate,
                    ReceivedDate = document.PostedAt ?? DateTime.UtcNow,
                    Status = BatchStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddBatchAsync(newPrdBatch);

                var prdAlloc = new BatchAllocation
                {
                    DocumentDetail = parentDetail,
                    Batch = newPrdBatch,
                    AllocationType = BatchAllocationType.ProductionReceipt,
                    QuantityAllocated = producedQty,
                    UnitCost = tpUnitCost,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddBatchAllocationAsync(prdAlloc);
            }
        }
        #endregion

        #region API Handlers cho Production
        public async Task<Models.Document> CreateProductionPendingAsync(Models.Document document, long userId)
        {
            await ValidateProductionBaseRulesAsync(document);
            var created = await CreatePendingBaseAsync(document, DocumentType.Production, userId);
            await GenerateOrValidateProductionCodeAsync(created);
            return created;
        }

        public async Task<Models.Document> CreateProductionCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.Production;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            await ValidateProductionBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateProductionCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessProductionCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<Models.Document> UpdateProductionPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.Production);
            await ValidateProductionBaseRulesAsync(existing);
            await GenerateOrValidateProductionCodeAsync(existing);
            return existing;
        }

        public async Task<Models.Document> CompleteProductionAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Production)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Sản xuất với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chốt chứng từ khi ở trạng thái Pending.");
            }

            await ValidateProductionBaseRulesAsync(existing);
            await GenerateOrValidateProductionCodeAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessProductionCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteProductionPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.Production, userId, deleteNote);
        }

        public async Task<IEnumerable<Models.Document>> GetProductionListAsync(long branchId)
        {
            return await _repo.GetProductionDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 8. Nghiệp vụ Chứng từ Kiểm kho (Check)

        #region Validation Nghiệp Vụ Phiếu Kiểm Kho (Check Validation Rules)
        /// <summary>
        /// Kiểm tra các quy tắc bắt buộc chung cho phiếu Kiểm kho (Check):
        /// - Note & DeleteNote <= 255 ký tự
        /// - Tối thiểu 1 chi tiết (DocumentDetail), không trùng BInventoryId
        /// - Product.Type != ProductType.Processed (chỉ cấm sản phẩm loại Processed)
        /// - Số lượng kiểm thực tế 0 <= Quantity <= 100 tỷ (cho phép số nhỏ hoặc 0, không cho phép số âm)
        /// - Snapshot SystemQuantity và ActualQuantity
        /// </summary>
        private async Task ValidateCheckBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ kiểm kho không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Tối thiểu 1 DocumentDetail
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu kiểm kho bắt buộc phải có tối thiểu 1 mặt hàng chi tiết (DocumentDetail).");
            }
            #endregion

            #region Quy tắc: Mỗi BInventoryId chỉ xuất hiện 1 lần
            var duplicateBInventory = document.DocumentDetails
                .GroupBy(d => d.BInventoryId)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateBInventory != null)
            {
                throw new MenuGoException(ErrorCodes.DocumentDuplicateDetail, $"Mặt hàng BInventory ID {duplicateBInventory.Key} bị trùng lặp trong phiếu kiểm kho. Mỗi mặt hàng chỉ được xuất hiện 1 lần.");
            }
            #endregion

            #region Quy tắc: Kiểm tra loại sản phẩm (!= Processed) và Số lượng (0 <= Quantity <= 100 tỷ)
            const decimal MIN_QTY = 0m;
            const decimal MAX_QTY = 100_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy mặt hàng BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                var productType = binventory.Product.Type;
                if (productType == ProductType.Processed)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' thuộc loại Processed (thành phẩm pha chế/chế biến) không được phép kiểm kho. Dạng phiếu này chỉ chấp nhận các loại sản phẩm ngoại trừ Processed.");
                }

                if (detail.Quantity == 0 && detail.ActualQuantity.HasValue && detail.ActualQuantity.Value > 0)
                {
                    detail.Quantity = detail.ActualQuantity.Value;
                }

                if (detail.Quantity < MIN_QTY || detail.Quantity > MAX_QTY)
                {
                    throw new MenuGoException(ErrorCodes.DocumentQuantityRangeExceeded, $"Số lượng kiểm thực tế của mặt hàng '{binventory.Product.Name}' ({detail.Quantity}) không hợp lệ. Số lượng phải nằm trong khoảng từ 0 đến 100 tỷ.");
                }

                #region Đơn vị tính và UnitConversion
                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion == null || unitConversion.ProductId != binventory.ProductId)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Đơn vị quy đổi ID {detail.UnitConversionId.Value} không tồn tại hoặc không thuộc về sản phẩm '{binventory.Product.Name}'.");
                    }

                    if (unitConversion.ConversionPoint <= 0)
                    {
                        throw new MenuGoException(ErrorCodes.DocumentUnitConversionNotFound, $"Tỷ lệ quy đổi của đơn vị tính ID {detail.UnitConversionId.Value} phải lớn hơn 0.");
                    }

                    detail.ConversionRate = unitConversion.ConversionPoint;
                }
                else
                {
                    detail.ConversionRate = 1;
                }
                #endregion

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
                detail.SystemQuantity = binventory.Quantity;
                detail.ActualQuantity = detail.BaseQuantity;
                detail.SnapshotAvgCost = binventory.Avg;
                detail.UnitPrice = binventory.Avg * detail.ConversionRate;
            }
            #endregion

            document.PartnerId = null;
            document.TotalAmount = document.DocumentDetails.Sum(d => (d.BaseQuantity - (d.SystemQuantity ?? 0m)) * d.SnapshotAvgCost);
            document.AmountPaid = 0;
            document.AmountDue = 0;
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Kiểm kho dựa trên ID (Tiền tố KK).
        /// </summary>
        private async Task GenerateOrValidateCheckCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"KK{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"KK{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Thực thi chốt phiếu Kiểm kho Completed:
        /// - Cập nhật tồn kho thực tế: binventory.Quantity = detail.ActualQuantity.Value
        /// - Nếu actual_q == 0 -> Triệt tiêu toàn bộ LeftOver cũ: binventory.LeftOver = 0
        /// - Ghi Sổ kho InventoryLedger (QuantityDelta = actual_q - q_system_old)
        /// </summary>
        private async Task ProcessCheckCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng BInventory ID: {detail.BInventoryId}");
                }

                if (detail.UnitConversionId.HasValue && detail.UnitConversionId.Value > 0)
                {
                    var unitConversion = await _repo.GetUnitConversionByIdAsync(detail.UnitConversionId.Value);
                    if (unitConversion != null && unitConversion.ConversionPoint > 0)
                    {
                        detail.ConversionRate = unitConversion.ConversionPoint;
                    }
                }
                if (detail.ConversionRate <= 0) detail.ConversionRate = 1;

                if (detail.Quantity == 0 && detail.ActualQuantity.HasValue && detail.ActualQuantity.Value > 0 && detail.ActualQuantity.Value != (detail.Quantity * detail.ConversionRate))
                {
                    detail.Quantity = detail.ActualQuantity.Value;
                }

                detail.BaseQuantity = detail.Quantity * detail.ConversionRate;
                detail.ActualQuantity = detail.BaseQuantity;
                detail.SystemQuantity = detail.SystemQuantity ?? binventory.Quantity;
                detail.SnapshotAvgCost = binventory.Avg;
                detail.UnitPrice = binventory.Avg * detail.ConversionRate;

                decimal actual_q = detail.BaseQuantity;
                decimal system_q = detail.SystemQuantity.Value;
                decimal q_delta = actual_q - system_q;

                decimal leftover_old = binventory.LeftOver;
                decimal val_delta;

                if (actual_q == 0)
                {
                    // Tồn kho thực tế về 0 -> Triệt tiêu toàn bộ LeftOver cũ
                    val_delta = -((system_q * binventory.Avg) + leftover_old);
                    binventory.Quantity = 0;
                    binventory.LeftOver = 0;
                }
                else
                {
                    binventory.Quantity = actual_q;
                    val_delta = q_delta * binventory.Avg;
                }

                // Bản ghi Sổ kho InventoryLedger
                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.Check,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = !string.IsNullOrWhiteSpace(detail.SnapshotProductName) ? detail.SnapshotProductName : (binventory.Product?.Name ?? string.Empty),
                    SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventory.Product?.Name ?? string.Empty),
                    ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                    QuantityDelta = q_delta,
                    InventoryValueDelta = val_delta,
                    RunningQuantity = binventory.Quantity,
                    RunningInventoryValue = binventory.Quantity == 0 ? 0m : (binventory.Quantity * binventory.Avg + binventory.LeftOver),
                    RunningAverageCost = binventory.Avg,
                    UnitCost = binventory.Avg,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);

                // Xử lý cập nhật Lô hàng (BInventoryBatch) và ghi nhận phân bổ Kiểm kho (StockCheck)
                if (!string.IsNullOrWhiteSpace(detail.BatchCodeSnapshot))
                {
                    var targetBatch = await _repo.GetBatchByInventoryAndCodeAsync(binventory.Id, detail.BatchCodeSnapshot);
                    if (targetBatch != null)
                    {
                        targetBatch.QuantityRemaining = actual_q;
                        if (actual_q == 0)
                        {
                            targetBatch.Status = BatchStatus.Depleted;
                        }
                        else if (targetBatch.Status == BatchStatus.Depleted)
                        {
                            targetBatch.Status = BatchStatus.Active;
                        }
                        targetBatch.UpdatedAt = DateTime.UtcNow;

                        var batchAlloc = new BatchAllocation
                        {
                            DocumentDetailId = detail.Id,
                            BatchId = targetBatch.Id,
                            AllocationType = BatchAllocationType.StockCheck,
                            QuantityAllocated = q_delta,
                            UnitCost = targetBatch.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAllocationAsync(batchAlloc);
                    }
                }
                else
                {
                    var batches = (await _repo.GetBatchesByInventoryIdAsync(binventory.Id)) ?? new List<BInventoryBatch>();
                    if (!batches.Any() && actual_q > 0)
                    {
                        var initBatch = new BInventoryBatch
                        {
                            BInventoryId = binventory.Id,
                            BatchCode = "LEGACY-INIT",
                            QuantityOriginal = actual_q,
                            QuantityRemaining = actual_q,
                            UnitCost = binventory.Avg,
                            ReceivedDate = document.PostedAt ?? DateTime.UtcNow,
                            Status = BatchStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _repo.AddBatchAsync(initBatch);
                    }
                    else if (batches.Any())
                    {
                        decimal currentTotalBatch = batches.Sum(b => b.QuantityRemaining);
                        decimal diff = actual_q - currentTotalBatch;
                        if (diff != 0)
                        {
                            var latestBatch = batches.OrderByDescending(b => b.ReceivedDate).FirstOrDefault(b => b.Status == BatchStatus.Active)
                                           ?? batches.OrderByDescending(b => b.ReceivedDate).First();
                            
                            latestBatch.QuantityRemaining = Math.Max(0, latestBatch.QuantityRemaining + diff);
                            if (latestBatch.QuantityRemaining == 0)
                            {
                                latestBatch.Status = BatchStatus.Depleted;
                            }
                            else if (latestBatch.Status == BatchStatus.Depleted)
                            {
                                latestBatch.Status = BatchStatus.Active;
                            }
                            latestBatch.UpdatedAt = DateTime.UtcNow;

                            var batchAlloc = new BatchAllocation
                            {
                                DocumentDetailId = detail.Id,
                                BatchId = latestBatch.Id,
                                AllocationType = BatchAllocationType.StockCheck,
                                QuantityAllocated = diff,
                                UnitCost = latestBatch.UnitCost,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _repo.AddBatchAllocationAsync(batchAlloc);
                        }
                    }
                }
            }

            document.TotalAmount = document.DocumentDetails.Sum(d => (d.BaseQuantity - (d.SystemQuantity ?? 0m)) * d.SnapshotAvgCost);
        }
        #endregion

        #region API Handlers cho Check
        public async Task<Models.Document> CreateCheckPendingAsync(Models.Document document, long userId)
        {
            await ValidateCheckBaseRulesAsync(document);
            var created = await CreatePendingBaseAsync(document, DocumentType.Check, userId);
            await GenerateOrValidateCheckCodeAsync(created);
            return created;
        }

        public async Task<Models.Document> CreateCheckCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.Check;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = string.Empty;
            }

            await ValidateCheckBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateCheckCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessCheckCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<Models.Document> UpdateCheckPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.Check);
            await ValidateCheckBaseRulesAsync(existing);
            await GenerateOrValidateCheckCodeAsync(existing);
            return existing;
        }

        public async Task<Models.Document> CompleteCheckAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.Check)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Kiểm kho với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chốt chứng từ khi ở trạng thái Pending.");
            }

            await ValidateCheckBaseRulesAsync(existing);
            await GenerateOrValidateCheckCodeAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessCheckCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteCheckPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.Check, userId, deleteNote);
        }

        public async Task<IEnumerable<Models.Document>> GetCheckListAsync(long branchId)
        {
            return await _repo.GetCheckDocumentsAsync(branchId);
        }
        #endregion

        #endregion

        #region 9. Nghiệp vụ Chứng từ Điều chỉnh giá vốn (CostAdjustment)

        #region Validation Nghiệp Vụ Phiếu Điều Chỉnh Giá Vốn (CostAdjustment Validation Rules cho Pending & Base)
        /// <summary>
        /// Kiểm tra quy tắc cơ bản cho phiếu Điều chỉnh giá vốn (CostAdjustment):
        /// - Note & DeleteNote <= 255 ký tự
        /// - Cấm sản phẩm loại ProductType.Processed (chế biến không có kho)
        /// - Giới hạn số tiền điều chỉnh (V_adj) từ -10,000,000,000 đến +10,000,000,000 (-10 tỷ đến +10 tỷ) và khác 0.
        /// - Áp dụng cả khi lưu tạm (Pending) và khi chốt (Completed).
        /// </summary>
        private async Task ValidateCostAdjustmentBaseRulesAsync(Models.Document document)
        {
            if (document == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentNullInfo, "Thông tin chứng từ điều chỉnh giá vốn không được để trống.");
            }

            #region Quy tắc: Note & DeleteNote không vượt quá 255 ký tự
            if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentNoteTooLong, "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.");
            }
            if (!string.IsNullOrEmpty(document.DeleteNote) && document.DeleteNote.Length > 255)
            {
                throw new MenuGoException(ErrorCodes.DocumentDeleteNoteTooLong, "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.");
            }
            #endregion

            #region Quy tắc: Danh sách mặt hàng điều chỉnh
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.DocumentMustHaveDetail, "Phiếu điều chỉnh giá vốn bắt buộc phải có ít nhất 1 mặt hàng.");
            }

            const decimal MIN_ADJ_VAL = -10_000_000_000m;
            const decimal MAX_ADJ_VAL = 10_000_000_000m;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductNotInInventory, $"Không tìm thấy kho hàng BInventory ID: {detail.BInventoryId}");
                }

                if (binventory.Product == null)
                {
                    throw new MenuGoException(ErrorCodes.DocumentInventoryNotLinked, $"BInventory ID {detail.BInventoryId} không liên kết với sản phẩm hợp lệ.");
                }

                if (binventory.Product.Type == ProductType.Processed)
                {
                    throw new MenuGoException(ErrorCodes.DocumentProductTypeNotAllowed, $"Sản phẩm '{binventory.Product.Name}' là loại Chế biến (Processed), không quản lý tồn kho và không được phép điều chỉnh giá vốn.");
                }

                decimal newAvgCostVal = detail.NewAvgCost ?? 0m;

                if (newAvgCostVal < 0 || newAvgCostVal > 10_000_000_000m)
                {
                    throw new MenuGoException(ErrorCodes.DocumentNewAvgCostOutOfRange, $"Giá vốn mới của sản phẩm '{binventory.Product.Name}' ({newAvgCostVal:N0} VNĐ) không hợp lệ. Phải nằm trong khoảng từ 0 đến 10 tỷ VNĐ.");
                }

                decimal q = binventory.Quantity;
                if (q <= 0)
                {
                    throw new MenuGoException(ErrorCodes.DocumentZeroInventoryForAdjustment, $"Không thể điều chỉnh giá vốn cho sản phẩm '{binventory.Product.Name}' vì số lượng tồn kho hiện tại không lớn hơn 0 (Tồn kho: {q}).");
                }

                // Nếu NewAvgCost chưa có nhưng có AdjustedCostDelta/UnitPrice (gọi từ code cũ), tự tính NewAvgCost
                if (!detail.NewAvgCost.HasValue || detail.NewAvgCost.Value == 0)
                {
                    if ((detail.AdjustedCostDelta.HasValue && detail.AdjustedCostDelta.Value != 0) || detail.UnitPrice != 0)
                    {
                        decimal oldVal = detail.AdjustedCostDelta ?? detail.UnitPrice;
                        newAvgCostVal = binventory.Avg + (oldVal / q);
                        detail.NewAvgCost = newAvgCostVal;
                    }
                }

                decimal avg_old = binventory.Avg;
                decimal v_adj = (newAvgCostVal - avg_old) * q;

                if (v_adj < MIN_ADJ_VAL || v_adj > MAX_ADJ_VAL)
                {
                    throw new MenuGoException(ErrorCodes.DocumentAdjustmentValueOutOfRange, $"Giá trị điều chỉnh giá vốn của sản phẩm '{binventory.Product.Name}' ({v_adj:N0} VNĐ) không hợp lệ. Hạn mức quy định từ -10 tỷ đến +10 tỷ VNĐ.");
                }

                // Không sử dụng quy đổi đơn vị tính cho điều chỉnh giá vốn
                detail.UnitConversionId = null;
                detail.ConversionRate = 1;
                detail.AdjustedCostDelta = v_adj;
                detail.UnitPrice = v_adj;
            }
            #endregion

            document.PartnerId = null;
            document.TotalAmount = 0;
            document.AmountPaid = 0;
            document.AmountDue = 0;
        }

        /// <summary>
        /// Phát sinh hoặc kiểm tra trùng lặp mã chứng từ cho phiếu Điều chỉnh giá vốn dựa trên ID (Tiền tố DC).
        /// </summary>
        private async Task GenerateOrValidateCostAdjustmentCodeAsync(Models.Document document)
        {
            if (!string.IsNullOrWhiteSpace(document.Code) && !document.Code.StartsWith("TEMP_"))
            {
                document.Code = document.Code.Trim();
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCodeDuplicate, $"Mã chứng từ '{document.Code}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
                }
            }
            else
            {
                document.Code = $"DC{document.Id:D6}";
                bool isExists = await _repo.IsCodeExistsAsync(document.Code, document.Id);
                if (isExists)
                {
                    document.Code = $"DC{document.Id:D6}_{DateTime.UtcNow.Ticks % 1000}";
                }
                _repo.Update(document);
                await _repo.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Thực thi chốt phiếu Điều chỉnh giá vốn Completed:
        /// 1. Kiểm tra số lượng tồn kho hiện tại (q > 0). Báo lỗi nếu q <= 0.
        /// 2. Kiểm tra giá vốn mới (NewAvgCost >= 0).
        /// 3. Tính chênh lệch tổng tiền kho theo yêu cầu: V_adj = (Giá mới - Giá cũ) * q.
        /// 4. Cập nhật BInventory.Avg = NewAvgCost, BInventory.LeftOver = 0.
        /// 5. Tạo bản ghi InventoryLedger với QuantityDelta = 0 và InventoryValueDelta = V_adj.
        /// </summary>
        private async Task ProcessCostAdjustmentCompletedAsync(Models.Document document, long userId)
        {
            if (document.DocumentDetails == null || document.DocumentDetails.Count == 0) return;

            foreach (var detail in document.DocumentDetails)
            {
                var binventory = await _repo.GetBInventoryByIdAsync(detail.BInventoryId);
                if (binventory == null) continue;

                string productName = detail.SnapshotProductName;
                if (string.IsNullOrWhiteSpace(productName) && binventory.Product != null)
                {
                    productName = binventory.Product.Name;
                }

                decimal q = binventory.Quantity;
                if (q <= 0)
                {
                    throw new MenuGoException(ErrorCodes.DocumentZeroInventoryForAdjustment, $"Không thể điều chỉnh giá vốn cho sản phẩm '{productName}' vì số lượng tồn kho hiện tại không lớn hơn 0 (Tồn kho: {q}).");
                }

                decimal newAvgCostVal = detail.NewAvgCost ?? 0m;

                if (newAvgCostVal < 0)
                {
                    throw new MenuGoException(ErrorCodes.DocumentCostAdjustmentNegative, $"Giá vốn mới của sản phẩm '{productName}' không được nhỏ hơn 0.");
                }

                decimal avg_old = binventory.Avg;
                decimal avg_new = newAvgCostVal;
                decimal v_adj = (avg_new - avg_old) * q;
                decimal v_new_total = avg_new * q;

                binventory.Avg = avg_new;
                binventory.LeftOver = 0m;

                detail.AdjustedCostDelta = v_adj;
                detail.UnitPrice = v_adj;
                detail.NewAvgCost = avg_new;
                detail.SnapshotAvgCost = avg_new;

                var ledger = new InventoryLedger
                {
                    BInventoryId = binventory.Id,
                    DocumentId = document.Id,
                    PostedAt = document.PostedAt ?? DateTime.UtcNow,
                    PostedBy = userId,
                    DocumentType = DocumentType.CostAdjustment,
                    SnapshotPostedByName = document.SnapshotPostedByName ?? string.Empty,
                    SnapshotBranchName = document.SnapshotBranchName ?? string.Empty,
                    SnapshotProductName = !string.IsNullOrWhiteSpace(productName) ? productName : (binventory.Product?.Name ?? string.Empty),
                    SnapshotUnitName = !string.IsNullOrWhiteSpace(detail.SnapshotUnitName) ? detail.SnapshotUnitName : (binventory.Product?.Name ?? string.Empty),
                    ConversionRateSnapshot = detail.ConversionRate > 0 ? detail.ConversionRate : 1m,
                    QuantityDelta = 0m,
                    InventoryValueDelta = v_adj,
                    RunningQuantity = q,
                    RunningInventoryValue = v_new_total,
                    RunningAverageCost = avg_new,
                    UnitCost = avg_new,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddInventoryLedgerAsync(ledger);
            }
        }
        #endregion

        #region API Handlers cho CostAdjustment
        public async Task<Models.Document> CreateCostAdjustmentPendingAsync(Models.Document document, long userId)
        {
            await ValidateCostAdjustmentBaseRulesAsync(document);
            var created = await CreatePendingBaseAsync(document, DocumentType.CostAdjustment, userId);
            await GenerateOrValidateCostAdjustmentCodeAsync(created);
            return created;
        }

        public async Task<Models.Document> CreateCostAdjustmentCompletedAsync(Models.Document document, long userId)
        {
            document.Type = DocumentType.CostAdjustment;
            document.Status = DocumentStatus.Completed;
            document.CreatedBy = userId;
            document.PostedBy = userId;
            document.CreatedAt = DateTime.UtcNow;
            document.PostedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = string.Empty;
            }

            await ValidateCostAdjustmentBaseRulesAsync(document);

            if (string.IsNullOrWhiteSpace(document.Code))
            {
                document.Code = $"TEMP_{Guid.NewGuid():N}";
            }

            await _repo.AddAsync(document);
            await _repo.SaveChangesAsync();

            await GenerateOrValidateCostAdjustmentCodeAsync(document);
            await FreezeDocumentSnapshotsAsync(document, userId, isSoftDelete: false);
            await ProcessCostAdjustmentCompletedAsync(document, userId);

            _repo.Update(document);
            await _repo.SaveChangesAsync();
            return document;
        }

        public async Task<Models.Document> UpdateCostAdjustmentPendingAsync(long documentId, Models.Document updatedDocument, long userId)
        {
            var existing = await UpdatePendingBaseAsync(documentId, updatedDocument, DocumentType.CostAdjustment);
            await ValidateCostAdjustmentBaseRulesAsync(existing);
            await GenerateOrValidateCostAdjustmentCodeAsync(existing);
            return existing;
        }

        public async Task<Models.Document> CompleteCostAdjustmentAsync(long documentId, long userId)
        {
            var existing = await _repo.GetByIdAsync(documentId);
            if (existing == null || existing.Type != DocumentType.CostAdjustment)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotFound, $"Không tìm thấy chứng từ Điều chỉnh giá vốn với ID: {documentId}");
            }

            ValidateDocumentNotLocked(existing);

            if (existing.Status != DocumentStatus.Pending)
            {
                throw new MenuGoException(ErrorCodes.DocumentNotPending, "Chỉ được phép chốt chứng từ khi ở trạng thái Pending.");
            }

            await ValidateCostAdjustmentBaseRulesAsync(existing);
            await GenerateOrValidateCostAdjustmentCodeAsync(existing);

            existing.Status = DocumentStatus.Completed;
            existing.PostedBy = userId;
            existing.PostedAt = DateTime.UtcNow;

            await FreezeDocumentSnapshotsAsync(existing, userId, isSoftDelete: false);
            await ProcessCostAdjustmentCompletedAsync(existing, userId);

            _repo.Update(existing);
            await _repo.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> SoftDeleteCostAdjustmentPendingAsync(long documentId, long userId, string deleteNote)
        {
            return await SoftDeletePendingBaseAsync(documentId, DocumentType.CostAdjustment, userId, deleteNote);
        }

        public async Task<IEnumerable<Models.Document>> GetCostAdjustmentListAsync(long branchId)
        {
            return await _repo.GetCostAdjustmentDocumentsAsync(branchId);
        }
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
    }
}
