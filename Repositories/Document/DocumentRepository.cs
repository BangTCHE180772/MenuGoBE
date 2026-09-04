using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Repositories.Document
{
    /// <summary>
    /// Triển khai Repository cho Module Document, xử lý các hàm riêng biệt cho từng loại chứng từ.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        #region Khai báo Fields và Constructor

        #region Context Db
        /// <summary>
        /// Context kết nối cơ sở dữ liệu ứng dụng.
        /// </summary>
        protected readonly AppDbContext _context;
        #endregion

        #region Constructor
        /// <summary>
        /// Khởi tạo DocumentRepository với AppDbContext.
        /// </summary>
        /// <param name="context">DbContext ứng dụng</param>
        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }
        #endregion

        #endregion

        #region Nghiệp vụ Thao tác Chung (Common Operations Implementation)

        #region Lấy chứng từ theo ID
        /// <summary>
        /// Lấy chi tiết thông tin chứng từ theo ID kèm theo đầy đủ thông tin liên quan (Partner, CashFlows, Details).
        /// </summary>
        public async Task<Models.Document?> GetByIdAsync(long id)
        {
            return await _context.Documents
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.BInventory)
                        .ThenInclude(b => b.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.UnitConversion)
                        .ThenInclude(uc => uc.Unit)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.BatchAllocations)
                .Include(d => d.Branch)
                .Include(d => d.ToBranch)
                .Include(d => d.Partner)
                    .ThenInclude(p => p.Address)
                .Include(d => d.CashFlows)
                .Include(d => d.InventoryLedgers)
                .Include(d => d.ParentDocument)
                .Include(d => d.Creator)
                .Include(d => d.Poster)
                .Include(d => d.Deleter)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Models.Document?> GetPendingDocumentByOrderIdAsync(long orderId, DocumentType type)
        {
            return await _context.Documents
                .Include(d => d.DocumentDetails)
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.Type == type && d.Status == DocumentStatus.Pending);
        }

        public async Task<Models.Document?> GetDocumentByOrderIdAsync(long orderId, DocumentType type)
        {
            return await _context.Documents
                .Include(d => d.DocumentDetails)
                .Include(d => d.CashFlows)
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.Type == type);
        }

        public async Task<Order?> GetOrderWithDetailsAsync(long orderId)
        {
            return await _context.Orders
                .Include(o => o.Table)
                    .ThenInclude(t => t.Area)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.RecipeItems)
                            .ThenInclude(ri => ri.IngredientProduct)
                .Include(o => o.ChildOrders)
                    .ThenInclude(co => co.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.RecipeItems)
                                .ThenInclude(ri => ri.IngredientProduct)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
        #endregion

        #region Kiểm tra chứng từ gốc (Parent Document)
        /// <summary>
        /// Kiểm tra chứng từ gốc hợp lệ, đúng loại và đã ở trạng thái Completed.
        /// </summary>
        public async Task<bool> ValidateParentDocumentAsync(long parentDocumentId, DocumentType expectedType)
        {
            return await _context.Documents.AnyAsync(d =>
                d.Id == parentDocumentId &&
                d.Type == expectedType &&
                d.Status == DocumentStatus.Completed);
        }
        #endregion

        #region Kiểm tra trùng mã chứng từ (Code Exists)
        /// <summary>
        /// Kiểm tra mã chứng từ đã tồn tại trong bảng Documents hay chưa.
        /// </summary>
        public async Task<bool> IsCodeExistsAsync(string code, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return await _context.Documents.AnyAsync(d => d.Code == code && (!excludeId.HasValue || d.Id != excludeId.Value));
        }
        #endregion

        #region Truy vấn Master Data (Branch, Partner, Account, BInventory & Đơn vị tính)
        /// <summary>
        /// Lấy thông tin Chi nhánh theo ID.
        /// </summary>
        public async Task<Branch?> GetBranchByIdAsync(long branchId)
        {
            return await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        }

        /// <summary>
        /// Lấy thông tin Đối tác/Nhà cung cấp theo ID.
        /// </summary>
        public async Task<Partner?> GetPartnerByIdAsync(long partnerId)
        {
            return await _context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId);
        }

        /// <summary>
        /// Lấy thông tin Tài khoản người dùng theo ID.
        /// </summary>
        public async Task<Account?> GetAccountByIdAsync(long accountId)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        }

        /// <summary>
        /// Lấy thông tin BInventory theo ID kèm Product và Branch.
        /// </summary>
        public async Task<BInventory?> GetBInventoryByIdAsync(long bInventoryId)
        {
            return await _context.BInventories
                .Include(b => b.Product)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == bInventoryId);
        }

        /// <summary>
        /// Lấy kho BInventory theo Chi nhánh và ProductId.
        /// </summary>
        public async Task<BInventory?> GetBInventoryByBranchAndProductAsync(long branchId, long productId)
        {
            return await _context.BInventories
                .Include(b => b.Product)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.BranchId == branchId && b.ProductId == productId);
        }

        /// <summary>
        /// Thêm mới bản ghi kho BInventory.
        /// </summary>
        public async Task AddBInventoryAsync(BInventory bInventory)
        {
            await _context.BInventories.AddAsync(bInventory);
        }

        /// <summary>
        /// Lấy quy đổi đơn vị tính theo ID kèm Unit.
        /// </summary>
        public async Task<UnitConversion?> GetUnitConversionByIdAsync(long unitConversionId)
        {
            return await _context.UnitConversions
                .Include(u => u.Unit)
                .FirstOrDefaultAsync(u => u.Id == unitConversionId);
        }

        /// <summary>
        /// Lấy danh sách công thức chi tiết theo Sản phẩm sản xuất (ParentProductId).
        /// </summary>
        public async Task<List<RecipesDetailed>> GetRecipesByParentProductIdAsync(long parentProductId)
        {
            return await _context.RecipesDetaileds
                .Include(r => r.IngredientProduct)
                .Where(r => r.ParentProductId == parentProductId)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy thông tin Sản phẩm theo ID kèm đơn vị quy đổi.
        /// </summary>
        public async Task<Product?> GetProductByIdAsync(long productId)
        {
            return await _context.Products
                .Include(p => p.UnitConversions)
                    .ThenInclude(uc => uc.Unit)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }
        #endregion

        #region Thêm mới CashFlow & InventoryLedger
        /// <summary>
        /// Thêm phiếu thu/chi CashFlow vào DbContext.
        /// </summary>
        public async Task AddCashFlowAsync(CashFlow cashFlow)
        {
            await _context.CashFlows.AddAsync(cashFlow);
        }

        /// <summary>
        /// Thêm bản ghi Sổ kho InventoryLedger vào DbContext.
        /// </summary>
        public async Task AddInventoryLedgerAsync(InventoryLedger ledger)
        {
            await _context.InventoryLedgers.AddAsync(ledger);
        }
        #endregion

        #region Thêm mới chứng từ
        /// <summary>
        /// Thêm chứng từ mới vào DbContext.
        /// </summary>
        public async Task AddAsync(Models.Document document)
        {
            await _context.Documents.AddAsync(document);
        }
        #endregion

        #region Cập nhật chứng từ
        /// <summary>
        /// Cập nhật chứng từ trong DbContext.
        /// </summary>
        public void Update(Models.Document document)
        {
            _context.Documents.Update(document);
        }

        /// <summary>
        /// Xóa danh sách chi tiết chứng từ (DocumentDetail) khỏi DbContext.
        /// </summary>
        public void RemoveDetails(IEnumerable<DocumentDetail> details)
        {
            if (details != null && details.Any())
            {
                _context.DocumentDetails.RemoveRange(details);
            }
        }
        #endregion

        #region Thao tác với Lô hàng (BInventoryBatch) & Phân bổ (BatchAllocation)
        public async Task<BInventoryBatch?> GetBatchByInventoryAndCodeAsync(long bInventoryId, string batchCode)
        {
            return await _context.BInventoryBatches
                .FirstOrDefaultAsync(b => b.BInventoryId == bInventoryId && b.BatchCode == batchCode);
        }

        public async Task<BInventoryBatch?> GetBatchByIdAsync(long batchId)
        {
            return await _context.BInventoryBatches.FirstOrDefaultAsync(b => b.Id == batchId);
        }

        public async Task<List<BInventoryBatch>> GetBatchesByInventoryIdAsync(long bInventoryId)
        {
            return await _context.BInventoryBatches
                .Where(b => b.BInventoryId == bInventoryId)
                .ToListAsync();
        }

        public async Task AddBatchAsync(BInventoryBatch batch)
        {
            await _context.BInventoryBatches.AddAsync(batch);
        }

        public async Task AddBatchAllocationAsync(BatchAllocation batchAllocation)
        {
            await _context.BatchAllocations.AddAsync(batchAllocation);
        }

        public async Task AddBatchAllocationsAsync(IEnumerable<BatchAllocation> batchAllocations)
        {
            await _context.BatchAllocations.AddRangeAsync(batchAllocations);
        }

        public async Task<List<BatchAllocation>> GetBatchAllocationsByDetailIdsAsync(IEnumerable<long> documentDetailIds)
        {
            return await _context.BatchAllocations
                .Include(a => a.Batch)
                .Where(a => documentDetailIds.Contains(a.DocumentDetailId))
                .ToListAsync();
        }
        #endregion

        #region Xóa mềm phiếu Pending
        /// <summary>
        /// Chuyển trạng thái chứng từ sang Canceled.
        /// </summary>
        public void SoftDeletePending(Models.Document document, long deletedBy, string deleteNote)
        {
            document.Status = DocumentStatus.Cancelled;
            document.DeletedBy = deletedBy;
            document.DeletedAt = DateTime.UtcNow;
            document.DeleteNote = deleteNote;
            _context.Documents.Update(document);
        }
        #endregion

        #region Lưu thay đổi DB
        /// <summary>
        /// Thực thi SaveChangesAsync đẩy dữ liệu xuống DB.
        /// </summary>
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
        #endregion

        #endregion

        #region Lấy danh sách chứng từ chung (Generic Get List)
        /// <summary>
        /// Lấy danh sách chứng từ tổng quan theo Chi nhánh, Loại chứng từ và Trạng thái.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetDocumentsAsync(long branchId, DocumentType? type = null, DocumentStatus? status = null)
        {
            var query = _context.Documents
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.BInventory)
                        .ThenInclude(b => b.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.UnitConversion)
                        .ThenInclude(uc => uc.Unit)
                .Include(d => d.Partner)
                    .ThenInclude(p => p.Address)
                .Include(d => d.CashFlows)
                .Include(d => d.ParentDocument)
                .Include(d => d.Creator)
                .Where(d => d.BranchId == branchId);

            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }

            return await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #region 1. Nghiệp vụ Chứng từ Nhập kho (Import)

        #region Lấy danh sách phiếu Nhập kho
        /// <summary>
        /// Lấy danh sách chứng từ Nhập kho thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetImportDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.BInventory)
                        .ThenInclude(b => b.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.UnitConversion)
                        .ThenInclude(uc => uc.Unit)
                .Include(d => d.Partner)
                    .ThenInclude(p => p.Address)
                .Include(d => d.CashFlows)
                .Include(d => d.Creator)
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Import)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 2. Nghiệp vụ Chứng từ Trả hàng NCC (Return)

        #region Lấy danh sách phiếu Trả hàng NCC
        /// <summary>
        /// Lấy danh sách chứng từ Trả hàng NCC thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetReturnDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Return)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #region Lấy tổng số lượng đã trả theo FatherId của phiếu nhập gốc
        /// <summary>
        /// Lấy tổng số lượng (BaseQuantity) đã trả tích lũy cho từng dòng chi tiết FatherId của phiếu nhập gốc.
        /// </summary>
        public async Task<Dictionary<long, decimal>> GetPreviouslyReturnedQuantitiesAsync(long parentImportDocumentId)
        {
            return await _context.DocumentDetails
                .Where(dd => dd.Document.ParentDocumentId == parentImportDocumentId
                         && dd.Document.Type == DocumentType.Return
                         && dd.Document.Status == DocumentStatus.Completed
                         && dd.FatherId.HasValue)
                .GroupBy(dd => dd.FatherId!.Value)
                .Select(g => new { FatherId = g.Key, TotalReturnedBaseQty = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.FatherId, x => x.TotalReturnedBaseQty);
        }
        #endregion

        #endregion

        #region 3. Nghiệp vụ Chứng từ Xuất bán hàng (Sale)

        #region Lấy danh sách phiếu Xuất bán hàng
        /// <summary>
        /// Lấy danh sách chứng từ Xuất bán hàng thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetSaleDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Sale)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 4. Nghiệp vụ Chứng từ Khách trả hàng (CustomerReturn)

        #region Lấy danh sách phiếu Khách trả hàng
        /// <summary>
        /// Lấy danh sách chứng từ Khách trả hàng thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetCustomerReturnDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.CustomerReturn)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 5. Nghiệp vụ Chứng từ Chuyển kho (Transfer)

        #region Lấy danh sách phiếu Chuyển kho
        /// <summary>
        /// Lấy danh sách chứng từ Chuyển kho thuộc chi nhánh gửi (hiển thị tất cả) hoặc chi nhánh nhận (chỉ hiển thị khi đã chốt Completed).
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetTransferDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.BInventory)
                        .ThenInclude(b => b.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                .Include(d => d.DocumentDetails)
                    .ThenInclude(dt => dt.UnitConversion)
                        .ThenInclude(uc => uc.Unit)
                .Include(d => d.Branch)
                .Include(d => d.ToBranch)
                .Include(d => d.Creator)
                .Where(d => d.Type == DocumentType.Transfer && (d.BranchId == branchId || (d.ToBranchId == branchId && d.Status == DocumentStatus.Completed)))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 6. Nghiệp vụ Chứng từ Xuất dùng nội bộ (Export)

        #region Lấy danh sách phiếu Xuất dùng nội bộ
        /// <summary>
        /// Lấy danh sách chứng từ Xuất dùng nội bộ thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetExportDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Export)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 6b. Nghiệp vụ Chứng từ Xuất hủy (ExportDelete)

        #region Lấy danh sách phiếu Xuất hủy
        /// <summary>
        /// Lấy danh sách chứng từ Xuất hủy thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetExportDeleteDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.ExportDelete)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 7. Nghiệp vụ Chứng từ Sản xuất (Production)

        #region Lấy danh sách phiếu Sản xuất
        /// <summary>
        /// Lấy danh sách chứng từ Sản xuất thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetProductionDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Production)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 8. Nghiệp vụ Chứng từ Kiểm kho (Check)

        #region Lấy danh sách phiếu Kiểm kho
        /// <summary>
        /// Lấy danh sách chứng từ Kiểm kho thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetCheckDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.Check)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion

        #region 9. Nghiệp vụ Chứng từ Điều chỉnh giá vốn (CostAdjustment)

        #region Lấy danh sách phiếu Điều chỉnh giá vốn
        /// <summary>
        /// Lấy danh sách chứng từ Điều chỉnh giá vốn thuộc chi nhánh.
        /// </summary>
        public async Task<IEnumerable<Models.Document>> GetCostAdjustmentDocumentsAsync(long branchId)
        {
            return await _context.Documents
                .Where(d => d.BranchId == branchId && d.Type == DocumentType.CostAdjustment)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        #endregion

        #endregion
    }
}
