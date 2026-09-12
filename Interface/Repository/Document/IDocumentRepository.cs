using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Interface.Repository.Document
{
    /// <summary>
    /// Interface Repository quản lý các hàm truy vấn và thao tác dữ liệu riêng biệt cho từng Loại chứng từ (Document Type).
    /// </summary>
    public interface IDocumentRepository
    {
        #region Nghiệp vụ Thao tác Chung (Common Operations)

        #region Lấy chứng từ theo ID & Lấy danh sách chứng từ chung
        /// <summary>
        /// Lấy chi tiết thông tin chứng từ theo ID.
        /// </summary>
        Task<Models.Document?> GetByIdAsync(long id);

        /// <summary>
        /// Lấy danh sách chứng từ tổng quan theo Chi nhánh, Loại chứng từ và Trạng thái.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetDocumentsAsync(long branchId, DocumentType? type = null, DocumentStatus? status = null);

        /// <summary>
        /// Lấy chứng từ Pending theo OrderId.
        /// </summary>
        Task<Models.Document?> GetPendingDocumentByOrderIdAsync(long orderId, DocumentType type);

        /// <summary>
        /// Lấy chứng từ bất kỳ theo OrderId và Type.
        /// </summary>
        Task<Models.Document?> GetDocumentByOrderIdAsync(long orderId, DocumentType type);

        /// <summary>
        /// Lấy Order đầy đủ OrderDetails và Recipes phục vụ trừ nguyên liệu.
        /// </summary>
        Task<Order?> GetOrderWithDetailsAsync(long orderId);
        #endregion

        #region Kiểm tra chứng từ gốc (Parent Document)
        /// <summary>
        /// Kiểm tra chứng từ gốc hợp lệ và ở trạng thái Completed.
        /// </summary>
        Task<bool> ValidateParentDocumentAsync(long parentDocumentId, DocumentType expectedType);
        #endregion

        #region Kiểm tra trùng mã chứng từ (Code Exists)
        /// <summary>
        /// Kiểm tra mã chứng từ đã tồn tại trong DB hay chưa.
        /// </summary>
        Task<bool> IsCodeExistsAsync(string code, long? excludeId = null);
        #endregion

        #region Truy vấn Master Data (Branch, Partner, Account, BInventory & Đơn vị tính)
        /// <summary>
        /// Lấy thông tin Chi nhánh theo ID.
        /// </summary>
        Task<Branch?> GetBranchByIdAsync(long branchId);

        /// <summary>
        /// Lấy thông tin Đối tác/Nhà cung cấp theo ID.
        /// </summary>
        Task<Partner?> GetPartnerByIdAsync(long partnerId);

        /// <summary>
        /// Lấy thông tin Tài khoản người dùng theo ID.
        /// </summary>
        Task<Account?> GetAccountByIdAsync(long accountId);

        /// <summary>
        /// Lấy thông tin BInventory theo ID.
        /// </summary>
        Task<BInventory?> GetBInventoryByIdAsync(long bInventoryId);

        /// <summary>
        /// Lấy kho BInventory theo Chi nhánh và ProductId.
        /// </summary>
        Task<BInventory?> GetBInventoryByBranchAndProductAsync(long branchId, long productId);

        /// <summary>
        /// Thêm mới bản ghi kho BInventory.
        /// </summary>
        Task AddBInventoryAsync(BInventory bInventory);

        /// <summary>
        /// Lấy quy đổi đơn vị tính theo ID kèm Unit.
        /// </summary>
        Task<UnitConversion?> GetUnitConversionByIdAsync(long unitConversionId);

        /// <summary>
        /// Lấy danh sách công thức chi tiết theo Sản phẩm sản xuất (ParentProductId).
        /// </summary>
        Task<List<RecipesDetailed>> GetRecipesByParentProductIdAsync(long parentProductId);

        /// <summary>
        /// Lấy thông tin Sản phẩm theo ID kèm đơn vị quy đổi.
        /// </summary>
        Task<Product?> GetProductByIdAsync(long productId);
        #endregion

        #region Thêm mới CashFlow & InventoryLedger
        /// <summary>
        /// Thêm bản ghi phiếu thu/chi CashFlow.
        /// </summary>
        Task AddCashFlowAsync(CashFlow cashFlow);

        /// <summary>
        /// Thêm bản ghi Sổ kho InventoryLedger.
        /// </summary>
        Task AddInventoryLedgerAsync(InventoryLedger ledger);
        #endregion

        #region Thêm mới chứng từ
        /// <summary>
        /// Thêm thực thể chứng từ vào DbContext.
        /// </summary>
        Task AddAsync(Models.Document document);
        #endregion

        #region Cập nhật chứng từ
        /// <summary>
        /// Đánh dấu cập nhật chứng từ trong DbContext.
        /// </summary>
        void Update(Models.Document document);

        /// <summary>
        /// Xóa danh sách chi tiết chứng từ (DocumentDetail) khỏi DbContext.
        /// </summary>
        void RemoveDetails(IEnumerable<DocumentDetail> details);
        #endregion

        #region Xóa mềm phiếu Pending
        /// <summary>
        /// Chuyển trạng thái chứng từ sang Canceled.
        /// </summary>
        void SoftDeletePending(Models.Document document, long deletedBy, string deleteNote);
        #endregion

        #region Thao tác với Lô hàng (BInventoryBatch) & Phân bổ (BatchAllocation)
        /// <summary>
        /// Lấy thông tin Lô theo BInventoryId và BatchCode.
        /// </summary>
        Task<BInventoryBatch?> GetBatchByInventoryAndCodeAsync(long bInventoryId, string batchCode);

        /// <summary>
        /// Lấy thông tin Lô theo Id.
        /// </summary>
        Task<BInventoryBatch?> GetBatchByIdAsync(long batchId);

        /// <summary>
        /// Lấy danh sách tất cả các Lô của một BInventory.
        /// </summary>
        Task<List<BInventoryBatch>> GetBatchesByInventoryIdAsync(long bInventoryId);

        /// <summary>
        /// Thêm mới một Lô hàng vào DbContext.
        /// </summary>
        Task AddBatchAsync(BInventoryBatch batch);

        /// <summary>
        /// Thêm mới một bản ghi phân bổ Lô vào DbContext.
        /// </summary>
        Task AddBatchAllocationAsync(BatchAllocation batchAllocation);

        /// <summary>
        /// Thêm mới danh sách bản ghi phân bổ Lô vào DbContext.
        /// </summary>
        Task AddBatchAllocationsAsync(IEnumerable<BatchAllocation> batchAllocations);

        /// <summary>
        /// Lấy danh sách phân bổ Lô theo danh sách DocumentDetailId.
        /// </summary>
        Task<List<BatchAllocation>> GetBatchAllocationsByDetailIdsAsync(IEnumerable<long> documentDetailIds);
        #endregion

        #region Lưu thay đổi DB
        /// <summary>
        /// Lưu tất cả thay đổi trong DbContext xuống Database.
        /// </summary>
        Task<bool> SaveChangesAsync();
        #endregion

        #endregion

        #region 1. Nghiệp vụ Chứng từ Nhập kho (Import)

        #region Lấy danh sách phiếu Nhập kho
        /// <summary>
        /// Lấy danh sách chứng từ Nhập kho theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetImportDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 2. Nghiệp vụ Chứng từ Trả hàng NCC (Return)

        #region Lấy danh sách phiếu Trả hàng NCC
        /// <summary>
        /// Lấy danh sách chứng từ Trả hàng NCC theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetReturnDocumentsAsync(long branchId);
        #endregion

        #region Lấy tổng số lượng đã trả theo FatherId của phiếu nhập gốc
        /// <summary>
        /// Lấy tổng số lượng (BaseQuantity) đã trả tích lũy cho từng dòng chi tiết FatherId của phiếu nhập gốc.
        /// </summary>
        Task<Dictionary<long, decimal>> GetPreviouslyReturnedQuantitiesAsync(long parentImportDocumentId);
        #endregion

        #endregion

        #region 3. Nghiệp vụ Chứng từ Xuất bán hàng (Sale)

        #region Lấy danh sách phiếu Xuất bán hàng
        /// <summary>
        /// Lấy danh sách chứng từ Xuất bán hàng theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetSaleDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 4. Nghiệp vụ Chứng từ Khách trả hàng (CustomerReturn)

        #region Lấy danh sách phiếu Khách trả hàng
        /// <summary>
        /// Lấy danh sách chứng từ Khách trả hàng theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCustomerReturnDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 5. Nghiệp vụ Chứng từ Chuyển kho (Transfer)

        #region Lấy danh sách phiếu Chuyển kho
        /// <summary>
        /// Lấy danh sách chứng từ Chuyển kho theo chi nhánh gửi.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetTransferDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 6. Nghiệp vụ Chứng từ Xuất dùng nội bộ (Export)

        #region Lấy danh sách phiếu Xuất dùng nội bộ
        /// <summary>
        /// Lấy danh sách chứng từ Xuất dùng nội bộ theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetExportDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 6b. Nghiệp vụ Chứng từ Xuất hủy (ExportDelete)

        #region Lấy danh sách phiếu Xuất hủy
        /// <summary>
        /// Lấy danh sách chứng từ Xuất hủy theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetExportDeleteDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 7. Nghiệp vụ Chứng từ Sản xuất (Production)

        #region Lấy danh sách phiếu Sản xuất
        /// <summary>
        /// Lấy danh sách chứng từ Sản xuất theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetProductionDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 8. Nghiệp vụ Chứng từ Kiểm kho (Check)

        #region Lấy danh sách phiếu Kiểm kho
        /// <summary>
        /// Lấy danh sách chứng từ Kiểm kho theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCheckDocumentsAsync(long branchId);
        #endregion

        #endregion

        #region 9. Nghiệp vụ Chứng từ Điều chỉnh giá vốn (CostAdjustment)

        #region Lấy danh sách phiếu Điều chỉnh giá vốn
        /// <summary>
        /// Lấy danh sách chứng từ Điều chỉnh giá vốn theo chi nhánh.
        /// </summary>
        Task<IEnumerable<Models.Document>> GetCostAdjustmentDocumentsAsync(long branchId);
        #endregion

        #endregion
    }
}
