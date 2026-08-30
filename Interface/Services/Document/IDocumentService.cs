using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Services.Document
{
    /// <summary>
    /// Interface Service định nghĩa các phương thức xử lý nghiệp vụ riêng biệt cho từng Loại chứng từ (Document Type).
    /// </summary>
    public interface IDocumentService
    {
        #region Quy tắc Kiểm tra & Khóa chứng từ chung (Common Validation Rules)

        #region Kiểm tra khóa tuyệt đối (Completed / Canceled Guard)
        /// <summary>
        /// Ràng buộc khóa tuyệt đối không cho phép sửa/xóa cứng phiếu đã Completed hoặc Canceled.
        /// </summary>
        void ValidateDocumentNotLocked(Models.Document document);
        #endregion

        #region Kiểm tra chứng từ gốc phụ thuộc (Parent Document Rule)
        /// <summary>
        /// Kiểm tra chứng từ gốc bắt buộc cho phiếu Return và CustomerReturn.
        /// </summary>
        Task ValidateParentDependencyRuleAsync(DocumentType type, long? parentDocumentId);
        #endregion

        #endregion

        #region Lấy DTO phản hồi dùng chung (Generic GET Response DTOs)
        /// <summary>
        /// Lấy thông tin chi tiết chứng từ bất kỳ dưới dạng DocumentResponseDto (Dùng chung cho xem chi tiết).
        /// </summary>
        Task<Dtos.Document.DocumentResponseDto?> GetDocumentByIdDtoAsync(long id);

        /// <summary>
        /// Lấy danh sách chứng từ dưới dạng DocumentListResponseDto (Dùng chung cho xem danh sách dạng bảng).
        /// </summary>
        Task<IEnumerable<Dtos.Document.DocumentListResponseDto>> GetDocumentListDtosAsync(long branchId, DocumentType? type = null, DocumentStatus? status = null);
        #endregion

        #region 1. Nghiệp vụ Chứng từ Nhập kho (Import)

        #region Tạo phiếu Nhập kho ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Nhập kho ở trạng thái Pending (Chưa lưu Snapshot, Chưa ghi sổ kho).
        /// </summary>
        Task<Models.Document> CreateImportPendingAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Nhập kho Pending
        /// <summary>
        /// Cập nhật phiếu Nhập kho đang ở trạng thái Pending (Cho phép sửa nhiều lần).
        /// </summary>
        Task<Models.Document> UpdateImportPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Nhập kho sang Completed
        /// <summary>
        /// Chốt phiếu Nhập kho sang Completed (Đổ bê tông Snapshot & Ghi sổ kho).
        /// </summary>
        Task<Models.Document> CompleteImportAsync(long documentId, long userId);
        #endregion

        #region Tạo & Chốt trực tiếp phiếu Nhập kho
        /// <summary>
        /// Tạo mới và Chốt trực tiếp phiếu Nhập kho (Completed).
        /// </summary>
        Task<Models.Document> CreateImportCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Xóa mềm phiếu Nhập kho Pending
        /// <summary>
        /// Xóa mềm phiếu Nhập kho Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteImportPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Nhập kho
        /// <summary>
        /// Lấy danh sách phiếu Nhập kho thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetImportListAsync(long branchId);
        #endregion

        #endregion

        #region 2. Nghiệp vụ Chứng từ Trả hàng NCC (Return)

        #region Tạo & Chốt trực tiếp phiếu Trả hàng NCC
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Trả hàng NCC (Phải trỏ về phiếu Import gốc ở trạng thái Completed).
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateReturnCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Lấy danh sách phiếu Trả hàng NCC
        /// <summary>
        /// Lấy danh sách phiếu Trả hàng NCC thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetReturnListAsync(long branchId);
        #endregion

        #region Lấy thông tin chi tiết phiếu Nhập kho cho tạo phiếu Trả hàng
        /// <summary>
        /// Lấy thông tin dòng chi tiết của phiếu Nhập kho (đã Completed) kèm số lượng đã trả tích lũy,
        /// phục vụ màn hình tạo phiếu Trả hàng NCC.
        /// </summary>
        Task<Dtos.Document.ImportReturnDetailsDto?> GetImportReturnDetailsAsync(long importDocumentId);
        #endregion

        #endregion

        #region 3. Nghiệp vụ Chứng từ Xuất bán hàng (Sale)

        #region Tạo & Chốt trực tiếp phiếu Xuất bán Completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất bán hàng (Mặc định Completed, trừ kho & ghi sổ kho).
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateSaleCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Tạo phiếu Xuất bán chỉ ghi nhận tài chính (không trừ kho)
        /// <summary>
        /// Tạo phiếu Xuất bán chỉ ghi nhận tài chính, KHÔNG trừ kho (dùng khi kho đã được trừ tại Confirm/Cooking).
        /// </summary>
        Task<Models.Document> CreateSaleRecordOnlyAsync(Models.Document document, long userId);
        #endregion

        #region Real-time Incremental Documents cho Order
        /// <summary>
        /// Lấy Document Pending của Order (Sale hoặc CustomerReturn). Nếu chưa có thì tạo mới.
        /// </summary>
        Task<Models.Document> GetOrCreatePendingDocumentAsync(long branchId, long orderId, DocumentType type, long userId);

        /// <summary>
        /// Thêm 1 item vào Sale Document (Pending). Thực hiện TRỪ KHO và Ghi Sổ Kho ngay lập tức.
        /// </summary>
        Task AppendItemToSaleDocumentAsync(long documentId, Product product, decimal baseQuantity, long parentDetailId, long userId);

        /// <summary>
        /// Thêm 1 item vào CustomerReturn Document (Pending). Thực hiện CỘNG KHO (nếu isIntact) và Ghi Sổ Kho ngay lập tức.
        /// </summary>
        Task AppendItemToReturnDocumentAsync(long documentId, Product product, decimal baseQuantity, bool isIntact, string returnReason, long parentDetailId, long userId);

        /// <summary>
        /// Chốt (Complete) tất cả Pending Documents của một Order khi thanh toán.
        /// Gán Customer, TotalAmount và ghi CashFlow.
        /// </summary>
        Task FinalizeOrderDocumentsAsync(long orderId, decimal finalAmount, MenuGoBE.Models.Enums.PaymentMethod paymentMethod, long? partnerId, long? userId);
        #endregion

        /// <summary>
        /// Lấy danh sách phiếu Xuất bán hàng thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetSaleListAsync(long branchId);
        #endregion

        #region 4. Nghiệp vụ Chứng từ Khách trả hàng (CustomerReturn)

        #region Tạo & Chốt trực tiếp phiếu Khách trả hàng
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Khách trả hàng (Phải trỏ về phiếu Sale gốc ở trạng thái Completed).
        /// KHÔNG hỗ trợ trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateCustomerReturnCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Lấy danh sách phiếu Khách trả hàng
        /// <summary>
        /// Lấy danh sách phiếu Khách trả hàng thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCustomerReturnListAsync(long branchId);
        #endregion

        #endregion

        #region 5. Nghiệp vụ Chứng từ Chuyển kho (Transfer)

        #region Tạo phiếu Chuyển kho ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Chuyển kho ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateTransferPendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo và Chốt trực tiếp phiếu Chuyển kho Completed (Bên gửi)
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Chuyển kho sang Completed (Trừ kho gửi & InTransit).
        /// </summary>
        Task<Models.Document> CreateTransferCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Chuyển kho Pending
        /// <summary>
        /// Cập nhật phiếu Chuyển kho ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateTransferPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Chuyển kho sang Completed (Bên gửi)
        /// <summary>
        /// Chốt phiếu Chuyển kho sang Completed (Trừ kho chi nhánh gửi, InTransit).
        /// </summary>
        Task<Models.Document> CompleteTransferAsync(long documentId, long userId);
        #endregion

        #region Xử lý xác nhận nhận hàng / từ chối hàng tại Chi nhánh Nhận
        /// <summary>
        /// Xử lý nhận hàng tại Chi nhánh Nhận theo DocumentTransferStatus (Received, PartialReceived, Rejected).
        /// </summary>
        Task<Models.Document> ProcessTransferReceiptAsync(long documentId, DocumentTransferStatus status, List<DocumentDetail>? receivedDetails, string? note, long userId);
        #endregion

        #region Xóa mềm phiếu Chuyển kho Pending
        /// <summary>
        /// Xóa mềm phiếu Chuyển kho Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteTransferPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Chuyển kho
        /// <summary>
        /// Lấy danh sách phiếu Chuyển kho thuộc chi nhánh gửi hoặc chi nhánh nhận.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetTransferListAsync(long branchId);
        #endregion
        #endregion

        #region 6. Nghiệp vụ Chứng từ Xuất hủy (Export)

        #region Tạo phiếu Xuất dùng nội bộ ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Xuất dùng nội bộ ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateExportPendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo phiếu Xuất dùng nội bộ ở trạng thái Completed trực tiếp
        /// <summary>
        /// Tạo mới và chốt trực tiếp phiếu Xuất dùng nội bộ sang Completed.
        /// </summary>
        Task<Models.Document> CreateExportCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Xuất hủy Pending
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateExportPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Xuất hủy sang Completed
        /// <summary>
        /// Chốt phiếu Xuất hủy sang Completed (Đổ bê tông Snapshot & Ghi giảm sổ kho).
        /// </summary>
        Task<Models.Document> CompleteExportAsync(long documentId, long userId);
        #endregion

        #region Xóa mềm phiếu Xuất hủy Pending
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteExportPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Xuất dùng nội bộ
        /// <summary>
        /// Lấy danh sách phiếu Xuất dùng nội bộ thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetExportListAsync(long branchId);
        #endregion

        #endregion

        #region 6b. Nghiệp vụ Chứng từ Xuất hủy (ExportDelete)

        #region Tạo phiếu Xuất hủy ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Xuất hủy ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateExportDeletePendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo và Chốt trực tiếp phiếu Xuất hủy Completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Xuất hủy sang Completed.
        /// </summary>
        Task<Models.Document> CreateExportDeleteCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Xuất hủy Pending
        /// <summary>
        /// Cập nhật phiếu Xuất hủy ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateExportDeletePendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Xuất hủy sang Completed
        /// <summary>
        /// Chốt phiếu Xuất hủy sang Completed (Đổ bê tông Snapshot, trừ kho & ném lỗi nếu vượt tồn kho).
        /// </summary>
        Task<Models.Document> CompleteExportDeleteAsync(long documentId, long userId);
        #endregion

        #region Xóa mềm phiếu Xuất hủy Pending
        /// <summary>
        /// Xóa mềm phiếu Xuất hủy Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteExportDeletePendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Xuất hủy
        /// <summary>
        /// Lấy danh sách phiếu Xuất hủy thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetExportDeleteListAsync(long branchId);
        #endregion

        #endregion

        #region 7. Nghiệp vụ Chứng từ Sản xuất (Production)

        #region Tạo phiếu Sản xuất ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Sản xuất ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateProductionPendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo và Chốt trực tiếp phiếu Sản xuất Completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Sản xuất sang Completed (Trừ kho NVL & Cộng kho TP).
        /// </summary>
        Task<Models.Document> CreateProductionCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Sản xuất Pending
        /// <summary>
        /// Cập nhật phiếu Sản xuất ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateProductionPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Sản xuất sang Completed
        /// <summary>
        /// Chốt phiếu Sản xuất sang Completed (Trừ kho NVL & Cộng kho thành phẩm).
        /// </summary>
        Task<Models.Document> CompleteProductionAsync(long documentId, long userId);
        #endregion

        #region Xóa mềm phiếu Sản xuất Pending
        /// <summary>
        /// Xóa mềm phiếu Sản xuất Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteProductionPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Sản xuất
        /// <summary>
        /// Lấy danh sách phiếu Sản xuất thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetProductionListAsync(long branchId);
        #endregion

        #endregion

        #region 8. Nghiệp vụ Chứng từ Kiểm kho (Check)

        #region Tạo phiếu Kiểm kho ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Kiểm kho ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateCheckPendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo và Chốt trực tiếp phiếu Kiểm kho Completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Kiểm kho sang Completed.
        /// </summary>
        Task<Models.Document> CreateCheckCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Kiểm kho Pending
        /// <summary>
        /// Cập nhật phiếu Kiểm kho ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateCheckPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Kiểm kho sang Completed
        /// <summary>
        /// Chốt phiếu Kiểm kho sang Completed (Cân bằng tồn thực tế & Điều chỉnh sổ kho).
        /// </summary>
        Task<Models.Document> CompleteCheckAsync(long documentId, long userId);
        #endregion

        #region Xóa mềm phiếu Kiểm kho Pending
        /// <summary>
        /// Xóa mềm phiếu Kiểm kho Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteCheckPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Kiểm kho
        /// <summary>
        /// Lấy danh sách phiếu Kiểm kho thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCheckListAsync(long branchId);
        #endregion

        #endregion

        #region 9. Nghiệp vụ Chứng từ Điều chỉnh giá vốn (CostAdjustment)

        #region Tạo phiếu Điều chỉnh giá vốn ở trạng thái Pending
        /// <summary>
        /// Tạo mới phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> CreateCostAdjustmentPendingAsync(Models.Document document, long userId);
        #endregion

        #region Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn Completed
        /// <summary>
        /// Tạo và Chốt trực tiếp phiếu Điều chỉnh giá vốn sang Completed.
        /// </summary>
        Task<Models.Document> CreateCostAdjustmentCompletedAsync(Models.Document document, long userId);
        #endregion

        #region Cập nhật phiếu Điều chỉnh giá vốn Pending
        /// <summary>
        /// Cập nhật phiếu Điều chỉnh giá vốn ở trạng thái Pending.
        /// </summary>
        Task<Models.Document> UpdateCostAdjustmentPendingAsync(long documentId, Models.Document updatedDocument, long userId);
        #endregion

        #region Chốt phiếu Điều chỉnh giá vốn sang Completed
        /// <summary>
        /// Chốt phiếu Điều chỉnh giá vốn sang Completed (Cập nhật đơn giá vốn mới cho hàng hóa).
        /// </summary>
        Task<Models.Document> CompleteCostAdjustmentAsync(long documentId, long userId);
        #endregion

        #region Xóa mềm phiếu Điều chỉnh giá vốn Pending
        /// <summary>
        /// Xóa mềm phiếu Điều chỉnh giá vốn Pending bằng cách chuyển sang trạng thái Canceled.
        /// </summary>
        Task<bool> SoftDeleteCostAdjustmentPendingAsync(long documentId, long userId, string deleteNote);
        #endregion

        #region Lấy danh sách phiếu Điều chỉnh giá vốn
        /// <summary>
        /// Lấy danh sách phiếu Điều chỉnh giá vốn thuộc chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCostAdjustmentListAsync(long branchId);
        #endregion

        #endregion
    }
}
